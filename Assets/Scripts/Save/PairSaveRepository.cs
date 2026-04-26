using UnityEngine;

namespace Save
{
    /// <summary>
    /// ペア単位セーブデータの永続化ストレージ。
    /// </summary>
    public static class PairSaveRepository
    {
        private const string KeyPrefix = "PairSave.";

        public static bool TryLoad(string pairKey, out PairSaveData data)
        {
            string json = PlayerPrefs.GetString(BuildSaveKey(pairKey), string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                data = null;
                return false;
            }

            data = JsonUtility.FromJson<PairSaveData>(json);
            return data != null;
        }

        public static void Save(PairSaveData data)
        {
            string key = BuildSaveKey(data.pairKey);
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        private static string BuildSaveKey(string pairKey)
        {
            return KeyPrefix + pairKey;
        }
    }
}