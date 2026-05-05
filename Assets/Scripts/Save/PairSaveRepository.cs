using UnityEngine;

namespace Save
{
    /// <summary>
    /// ペア単位セーブデータの永続化ストレージ。
    /// </summary>
    public static class PairSaveRepository
    {
        private const string KeyPrefix = "PairSave.";
        private const string IndexPrefix = "PairSave.Index.";

        public static bool TryLoad(string slotKey, out PairSaveData data)
        {
            string json = PlayerPrefs.GetString(BuildSaveKey(slotKey), string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                data = null;
                return false;
            }

            data = JsonUtility.FromJson<PairSaveData>(json);
            return data != null;
        }

        public static bool TryLoadLatest(string pairKey, out PairSaveData data, out string slotKey)
        {
            if (!TryGetLastSlotKey(pairKey, out slotKey))
            {
                data = null;
                return false;
            }

            return TryLoad(slotKey, out data);
        }

        public static void Save(string slotKey, PairSaveData data)
        {
            string key = BuildSaveKey(slotKey);
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(key, json);
            SetLastSlotKey(data.pairKey, slotKey);
            PlayerPrefs.Save();
        }

        public static void Delete(string slotKey)
        {
            PlayerPrefs.DeleteKey(BuildSaveKey(slotKey));
            PlayerPrefs.Save();
        }

        public static void ClearPairIndex(string pairKey)
        {
            PlayerPrefs.DeleteKey(BuildIndexKey(pairKey));
            PlayerPrefs.Save();
        }

        private static bool TryGetLastSlotKey(string pairKey, out string slotKey)
        {
            slotKey = PlayerPrefs.GetString(BuildIndexKey(pairKey), string.Empty);
            return !string.IsNullOrEmpty(slotKey);
        }

        private static void SetLastSlotKey(string pairKey, string slotKey)
        {
            PlayerPrefs.SetString(BuildIndexKey(pairKey), slotKey);
        }

        private static string BuildSaveKey(string slotKey)
        {
            return KeyPrefix + slotKey;
        }

        private static string BuildIndexKey(string pairKey)
        {
            return IndexPrefix + pairKey;
        }
    }
}