using System.IO;
using UnityEditor;
using UnityEngine;

namespace RunconaLib.Audio.Editor
{
    public static class AudioDatabaseCreator
    {
        private const string ResourcesDirectory = "Assets/Resources";
        private const string AssetPath = ResourcesDirectory + "/AudioDatabase.asset";
        private const string MenuPath = "Tools/RunconaLib/Audio/Create Audio Database";

        [MenuItem(MenuPath)]
        private static void CreateAudioDatabase()
        {
            AudioDatabase existingDatabase = AssetDatabase.LoadAssetAtPath<AudioDatabase>(AssetPath);
            if (existingDatabase != null)
            {
                SelectAsset(existingDatabase);
                Debug.Log($"AudioDatabase は既に存在します: {AssetPath}", existingDatabase);
                return;
            }

            Directory.CreateDirectory(ResourcesDirectory);

            AudioDatabase database = ScriptableObject.CreateInstance<AudioDatabase>();
            AssetDatabase.CreateAsset(database, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SelectAsset(database);
            Debug.Log($"AudioDatabase を作成しました: {AssetPath}", database);
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateCreateAudioDatabase() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static void SelectAsset(AudioDatabase database)
        {
            Selection.activeObject = database;
            EditorGUIUtility.PingObject(database);
        }
    }
}