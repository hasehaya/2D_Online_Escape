using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunconaLib.Localization
{
    [CreateAssetMenu(menuName = "Escape/Localization/Table", fileName = "LocalizationTable")]
    public sealed class LocalizationTable : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string key;
            [TextArea] public string japanese;
            [TextArea] public string english;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();
        private Dictionary<string, Entry> _lookup;

        public IReadOnlyList<Entry> Entries => _entries;

        public string Get(string key, int languageIndex)
        {
            EnsureLookup();
            if (!_lookup.TryGetValue(key, out Entry entry)) return key;
            string value = languageIndex == 1 ? entry.english : entry.japanese;
            return string.IsNullOrEmpty(value) ? entry.japanese : value;
        }

        public void ReplaceEntries(IEnumerable<Entry> entries)
        {
            _entries = new List<Entry>(entries);
            _lookup = null;
        }

        private void OnEnable() => _lookup = null;

        private void EnsureLookup()
        {
            if (_lookup != null) return;
            _lookup = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (Entry entry in _entries)
                if (entry != null && !string.IsNullOrWhiteSpace(entry.key))
                    _lookup[entry.key] = entry;
        }
    }
}