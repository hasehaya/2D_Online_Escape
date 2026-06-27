using Escape.SceneObject.Noel.Prepare;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(BookShelfObject))]
    public class BookShelfObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            BookShelfObject bookShelfObject = (BookShelfObject)target;

            using (new EditorGUI.DisabledScope(!Application.isPlaying || bookShelfObject.IsSlid))
            {
                if (GUILayout.Button("Slide Left"))
                {
                    bookShelfObject.RequestSlide();
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test the bookshelf slide.", MessageType.Info);
            }
            else if (bookShelfObject.IsSlid)
            {
                EditorGUILayout.HelpBox("This bookshelf has already moved.", MessageType.Info);
            }
        }
    }
}