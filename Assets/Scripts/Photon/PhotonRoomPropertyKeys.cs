using System;

public static class PhotonRoomPropertyKeys
{
    public const string FlagPrefix = "Flag_";
    public const string TransientPrefix = "Transient.";

    public const string PlayerId = "PlayerId";
    public const string SavePairKey = "Save.PairKey";
    public const string SaveEliasPlayerId = "Save.EliasPlayerId";
    public const string SaveNoelPlayerId = "Save.NoelPlayerId";

    public const string InventorySharedSlotUnlocked = "Inventory.SharedSlot.Unlocked";
    public const string InventorySharedSlotItem = "Inventory.SharedSlot.Item";
    public const string InventoryHasMagicSack = "Inventory.HasMagicSack";

    public const string WakeLaserDistanceRatio = "LaserDistanceRatio";
    public const string DungeonLightsOutPuzzleBoardBits = TransientPrefix + "DungeonLightsOutPuzzle.BoardBits";
    public const string SharedStillTransition = TransientPrefix + "View.SharedStillTransition";

    public static string BuildFlagKey(FlagType flag)
    {
        return $"{FlagPrefix}{flag}";
    }

    public static bool IsFlagKey(string key)
    {
        return !string.IsNullOrEmpty(key)
               && key.StartsWith(FlagPrefix, StringComparison.Ordinal);
    }

    public static bool TryParseFlagKey(string key, out FlagType flag)
    {
        flag = FlagType.None;
        if (!IsFlagKey(key))
        {
            return false;
        }

        string flagName = key.Substring(FlagPrefix.Length);
        return Enum.TryParse(flagName, out flag);
    }

    public static bool IsTransientKey(string key)
    {
        return !string.IsNullOrEmpty(key)
               && key.StartsWith(TransientPrefix, StringComparison.Ordinal);
    }

    public static bool IsSessionKey(string key)
    {
        return key == PlayerId
               || key == SavePairKey
               || key == SaveEliasPlayerId
               || key == SaveNoelPlayerId;
    }

    public static bool IsRealtimeSyncKey(string key)
    {
        return key == WakeLaserDistanceRatio;
    }

    public static bool IsPersistentSharedProgressKey(string key)
    {
        return !string.IsNullOrEmpty(key)
               && !IsTransientKey(key)
               && !IsSessionKey(key)
               && !IsRealtimeSyncKey(key);
    }
}
