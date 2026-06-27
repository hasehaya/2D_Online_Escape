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
    /// 現在ルームからセーブスロットとローカル役割を解決する。
    /// </summary>
    public static class SaveSessionContext
    {
        public static string GetCurrentPairKey()
        {
            if (!PhotonNetwork.InRoom)
            {
                return string.Empty;
            }

            object value;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PhotonRoomPropertyKeys.SavePairKey, out value))
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
            string eliasId = GetRoomString(PhotonRoomPropertyKeys.SaveEliasPlayerId);
            string noelId = GetRoomString(PhotonRoomPropertyKeys.SaveNoelPlayerId);

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

        public static string GetCurrentSaveSlotKey()
        {
            string pairKey = GetCurrentPairKey();
            if (string.IsNullOrEmpty(pairKey))
            {
                return string.Empty;
            }

            string eliasId = GetRoomString(PhotonRoomPropertyKeys.SaveEliasPlayerId);
            string noelId = GetRoomString(PhotonRoomPropertyKeys.SaveNoelPlayerId);
            if (string.IsNullOrEmpty(eliasId) || string.IsNullOrEmpty(noelId))
            {
                return string.Empty;
            }

            return BuildSaveSlotKey(pairKey, eliasId, noelId);
        }

        public static string BuildSaveSlotKey(string pairKey, string eliasPlayerId, string noelPlayerId)
        {
            return pairKey + "__E_" + eliasPlayerId + "__N_" + noelPlayerId;
        }
    }
}