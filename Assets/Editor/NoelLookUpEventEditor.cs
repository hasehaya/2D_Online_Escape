using Noel;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(NoelLookUpEvent))]
    public class NoelLookUpEventEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            NoelLookUpEvent lookUpEvent = (NoelLookUpEvent)target;

            if (GUILayout.Button("Play Look Up Sequence"))
            {
                if (Application.isPlaying)
                {
                    lookUpEvent.PlayLookUpSequence();
                }
                else
                {
                    EditorGUILayout.HelpBox("Play mode required to test this sequence.", MessageType.Warning);
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test the Look Up Sequence", MessageType.Info);
            }
        }
    }
}