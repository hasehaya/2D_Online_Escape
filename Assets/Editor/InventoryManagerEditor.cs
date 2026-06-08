using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(InventoryManager))]
    public class InventoryManagerEditor : UnityEditor.Editor
    {
        private ItemType _itemToAdd = GetDefaultItemType();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space();

            DrawDebugInitialStateSection();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Actions", EditorStyles.boldLabel);

            _itemToAdd = DrawAddItemField(_itemToAdd);

            using (new EditorGUI.DisabledScope(!Application.isPlaying || _itemToAdd == ItemType.None))
            {
                if (GUILayout.Button("選択したアイテムを追加"))
                {
                    InventoryManager inventoryManager = (InventoryManager)target;
                    if (!inventoryManager.TryAddItem(_itemToAdd))
                    {
                        Debug.LogWarning($"[InventoryManagerEditor] インベントリが満杯のため {_itemToAdd} を追加できませんでした。");
                    }
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode 中にインベントリへアイテムを追加できます。", MessageType.Info);
            }
            else if (_itemToAdd == ItemType.None)
            {
                EditorGUILayout.HelpBox("追加するアイテムを選択してください。", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDebugInitialStateSection()
        {
            InventoryManager inventoryManager = (InventoryManager)target;

            EditorGUILayout.LabelField("Debug Initial State", EditorStyles.boldLabel);

            SerializedProperty useDebugInitialState = serializedObject.FindProperty("_useDebugInitialState");
            SerializedProperty debugInitialItems = serializedObject.FindProperty("_debugInitialItems");

            EditorGUILayout.PropertyField(useDebugInitialState, new GUIContent("Enable Debug Initial State"));
            EditorGUILayout.PropertyField(debugInitialItems, new GUIContent("Initial Items"), true);

            if (GUILayout.Button("現在の所持品をDebug初期状態として保存"))
            {
                Undo.RecordObject(inventoryManager, "Save Debug Initial Inventory");

                debugInitialItems.arraySize = 0;

                IReadOnlyList<ItemType> currentItems = inventoryManager.GetItems();
                for (int i = 0; i < currentItems.Count; i++)
                {
                    debugInitialItems.InsertArrayElementAtIndex(i);
                    debugInitialItems.GetArrayElementAtIndex(i).enumValueIndex = (int)currentItems[i];
                }

                useDebugInitialState.boolValue = true;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(inventoryManager);
            }

            if (Application.isPlaying && GUILayout.Button("Debug初期状態を現在のインベントリへ適用"))
            {
                inventoryManager.SetItems(ReadDebugInitialItems(debugInitialItems));
            }

            EditorGUILayout.HelpBox(
                "Enable Debug Initial State を有効にすると、再生開始時に Initial Items がインベントリ初期値として適用されます。",
                MessageType.Info);
        }

        private static ItemType DrawAddItemField(ItemType current)
        {
            return (ItemType)EditorGUILayout.EnumPopup("追加するアイテム", current);
        }

        private static List<ItemType> ReadDebugInitialItems(SerializedProperty debugInitialItems)
        {
            List<ItemType> items = new List<ItemType>();

            for (int i = 0; i < debugInitialItems.arraySize; i++)
            {
                items.Add((ItemType)debugInitialItems.GetArrayElementAtIndex(i).enumValueIndex);
            }

            return items;
        }

        private static ItemType GetDefaultItemType()
        {
            Array values = Enum.GetValues(typeof(ItemType));
            for (int i = 0; i < values.Length; i++)
            {
                ItemType itemType = (ItemType)values.GetValue(i);
                if (itemType != ItemType.None)
                {
                    return itemType;
                }
            }

            return ItemType.None;
        }
    }
}