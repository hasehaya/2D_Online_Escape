using Photon.Pun;

namespace Save
{
    public enum SaveRole
    {
        Unknown = 0,
        Elias = 1,
        Noel = 2
    }

    /// <summary>
    /// ルーム上のセーブ関連キーとローカル役割判定を扱う。
    /// </summary>
    public static class SaveSessionContext
    {
        public const string PlayerIdPropertyKey = "PlayerId";
        public const string PairKeyRoomPropertyKey = "Save.PairKey";
        public const string EliasPlayerIdRoomPropertyKey = "Save.EliasPlayerId";
        public const string NoelPlayerIdRoomPropertyKey = "Save.NoelPlayerId";

        public static string GetCurrentPairKey()
        {
            if (!PhotonNetwork.InRoom)
            {
                return string.Empty;
            }

            object value;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PairKeyRoomPropertyKey, out value))
            {
                return value as string ?? string.Empty;
            }

            return string.Empty;
        }

        public static SaveRole GetLocalRole()
        {
            if (!PhotonNetwork.InRoom)
            {
                return SaveRole.Unknown;
            }

            string localId = LocalIdentityProvider.GetOrCreateLocalPlayerId();
            string eliasId = GetRoomString(EliasPlayerIdRoomPropertyKey);
            string noelId = GetRoomString(NoelPlayerIdRoomPropertyKey);

            if (localId == eliasId)
            {
                return SaveRole.Elias;
            }

            if (localId == noelId)
            {
                return SaveRole.Noel;
            }

            return SaveRole.Unknown;
        }

        public static string BuildPairKey(string playerIdA, string playerIdB)
        {
            if (string.CompareOrdinal(playerIdA, playerIdB) <= 0)
            {
                return playerIdA + "__" + playerIdB;
            }

            return playerIdB + "__" + playerIdA;
        }

        public static string GetRoomString(string key)
        {
            if (!PhotonNetwork.InRoom)
            {
                return string.Empty;
            }

            object value;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out value))
            {
                return value as string ?? string.Empty;
            }

            return string.Empty;
        }
    }
}