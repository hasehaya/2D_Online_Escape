using System;
using System.Collections.Generic;

namespace Save
{
    [Serializable]
    public class PairSaveData
    {
        public string pairKey;
        public string eliasPlayerId;
        public string noelPlayerId;
        public SharedProgressData sharedProgress = new SharedProgressData();
        public RoleSaveData elias = new RoleSaveData();
        public RoleSaveData noel = new RoleSaveData();

        public RoleSaveData GetRoleData(SaveRole role)
        {
            return role == SaveRole.Noel ? noel : elias;
        }
    }

    [Serializable]
    public class RoleSaveData
    {
        public string sceneName;
        public long savedAtTicks;
        public List<string> inventoryItemIds = new List<string>();
        public List<SaveableObjectData> saveables = new List<SaveableObjectData>();
    }

    [Serializable]
    public class SharedProgressData
    {
        public List<BoolEntry> bools = new List<BoolEntry>();
        public List<FloatEntry> floats = new List<FloatEntry>();
        public List<IntEntry> ints = new List<IntEntry>();
        public List<StringEntry> strings = new List<StringEntry>();
    }

    [Serializable]
    public class SaveableObjectData
    {
        public string saveId;
        public string stateJson;
    }

    [Serializable]
    public class BoolEntry
    {
        public string key;
        public bool value;
    }

    [Serializable]
    public class FloatEntry
    {
        public string key;
        public float value;
    }

    [Serializable]
    public class IntEntry
    {
        public string key;
        public int value;
    }

    [Serializable]
    public class StringEntry
    {
        public string key;
        public string value;
    }
}