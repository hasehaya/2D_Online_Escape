using System;
using UnityEngine;

namespace Save
{
    /// <summary>
    /// Steam連携前の暫定ローカルプレイヤーIDを提供する。
    /// </summary>
    public static class LocalIdentityProvider
    {
        private const string LocalPlayerIdKey = "Save.TempLocalPlayerId";

        public static string GetOrCreateLocalPlayerId()
        {
            string id = PlayerPrefs.GetString(LocalPlayerIdKey, string.Empty);
            if (!string.IsNullOrEmpty(id))
            {
                return id;
            }

            id = $"temp_{Guid.NewGuid():N}";
            PlayerPrefs.SetString(LocalPlayerIdKey, id);
            PlayerPrefs.Save();
            return id;
        }
    }
}