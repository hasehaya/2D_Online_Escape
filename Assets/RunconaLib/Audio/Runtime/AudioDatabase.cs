using System;
using UnityEngine;

namespace RunconaLib.Audio
{
    [CreateAssetMenu(fileName = "AudioDatabase", menuName = "RunconaLib/Audio/Audio Database")]
    public sealed class AudioDatabase : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string _id;
            [SerializeField] private AudioClip _clip;
            public string Id => _id;
            public AudioClip Clip => _clip;
        }

        [SerializeField] private Entry[] _seEntries = Array.Empty<Entry>();
        [SerializeField] private Entry[] _bgmEntries = Array.Empty<Entry>();

        public bool TryGetSEClip(string id, out AudioClip clip) => TryGetClip(_seEntries, id, out clip);
        public bool TryGetBGMClip(string id, out AudioClip clip) => TryGetClip(_bgmEntries, id, out clip);

        private static bool TryGetClip(Entry[] entries, string id, out AudioClip clip)
        {
            if (!string.IsNullOrEmpty(id) && entries != null)
                foreach (Entry entry in entries)
                    if (entry != null && string.Equals(entry.Id, id, StringComparison.Ordinal))
                    {
                        clip = entry.Clip;
                        return clip != null;
                    }

            clip = null;
            return false;
        }
    }
}