using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(ItemDatabase))]
    public class ItemDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (GUILayout.Button("EnumからListを自動生成"))
            {
                GenerateItemsFromEnum();
            }

            DrawIconWarnings();

            serializedObject.ApplyModifiedProperties();
        }

        private void GenerateItemsFromEnum()
        {
            ItemDatabase database = (ItemDatabase)target;
            Dictionary<ItemType, Sprite> iconMap = BuildIconMap(database.Items);
            List<ItemType> itemTypes = BuildItemTypeList();

            Undo.RecordObject(database, "Generate Item Database From Enum");

            SerializedProperty itemsProperty = serializedObject.FindProperty("_items");
            itemsProperty.arraySize = itemTypes.Count;

            for (int i = 0; i < itemTypes.Count; i++)
            {
                SerializedProperty element = itemsProperty.GetArrayElementAtIndex(i);
                ItemType itemType = itemTypes[i];

                SetEnumProperty(element.FindPropertyRelative("itemType"), itemType);

                SerializedProperty iconProperty = element.FindPropertyRelative("icon");
                Sprite icon;
                iconProperty.objectReferenceValue = iconMap.TryGetValue(itemType, out icon) ? icon : null;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
        }

        private void DrawIconWarnings()
        {
            ItemDatabase database = (ItemDatabase)target;
            List<string> missingIcons = new List<string>();

            IReadOnlyList<ItemData> items = database.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemData item = items[i];
                if (item == null)
                {
                    continue;
                }

                if (item.icon == null)
                {
                    missingIcons.Add(item.itemType.ToString());
                }
            }

            if (missingIcons.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"{missingIcons.Count}件のアイテムに画像が設定されていません: {string.Join(", ", missingIcons)}",
                    MessageType.Warning);
            }
        }

        private static Dictionary<ItemType, Sprite> BuildIconMap(IReadOnlyList<ItemData> items)
        {
            Dictionary<ItemType, Sprite> iconMap = new Dictionary<ItemType, Sprite>();

            for (int i = 0; i < items.Count; i++)
            {
                ItemData item = items[i];
                if (item == null || iconMap.ContainsKey(item.itemType))
                {
                    continue;
                }

                iconMap.Add(item.itemType, item.icon);
            }

            return iconMap;
        }

        private static List<ItemType> BuildItemTypeList()
        {
            List<ItemType> itemTypes = new List<ItemType>();
            Array values = Enum.GetValues(typeof(ItemType));

            for (int i = 0; i < values.Length; i++)
            {
                ItemType itemType = (ItemType)values.GetValue(i);
                if (itemType == ItemType.None)
                {
                    continue;
                }

                itemTypes.Add(itemType);
            }

            return itemTypes;
        }

        private static void SetEnumProperty(SerializedProperty property, ItemType itemType)
        {
            string[] names = Enum.GetNames(typeof(ItemType));
            property.enumValueIndex = Array.IndexOf(names, itemType.ToString());
        }
    }
}