using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(InteractableObject), true)]
    public class InteractableObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            InteractableObject interactableObject = (InteractableObject)target;

            if (GUILayout.Button("現在のRectTransformに合わせる"))
            {
                if (interactableObject.TryGetComponent(out RectTransform rectTransform))
                {
                    Undo.RecordObject(interactableObject, "Match Click Area To RectTransform");

                    SerializedObject so = serializedObject;
                    so.Update();

                    so.FindProperty("_clickAreaSize").vector2Value = rectTransform.rect.size;
                    so.FindProperty("_clickAreaOffset").vector2Value = rectTransform.rect.center;
                    so.FindProperty("_clickAreaInitialized").boolValue = true;

                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(interactableObject);
                }
                else
                {
                    Debug.LogWarning(
                        $"[InteractableObjectEditor] {interactableObject.name} に RectTransform がないため、サイズを合わせられません。");
                }
            }

            if (!interactableObject.TryGetComponent(out RectTransform _))
            {
                EditorGUILayout.HelpBox("このボタンは RectTransform を持つ UI オブジェクトで使用できます。", MessageType.Info);
            }
        }
    }
}