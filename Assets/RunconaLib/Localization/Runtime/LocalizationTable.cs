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
        private Dictionary<string, string> _keyByText;

        public IReadOnlyList<Entry> Entries => _entries;

        public string Get(string key, int languageIndex)
        {
            EnsureLookup();
            if (!_lookup.TryGetValue(key, out Entry entry)) return key;
            string value = languageIndex == 1 ? entry.english : entry.japanese;
            return string.IsNullOrEmpty(value) ? entry.japanese : value;
        }

        public bool TryGetKey(string text, out string key)
        {
            EnsureLookup();
            return _keyByText.TryGetValue(text ?? string.Empty, out key);
        }

        public void ReplaceEntries(IEnumerable<Entry> entries)
        {
            _entries = new List<Entry>(entries);
            _lookup = null;
            _keyByText = null;
        }

        private void OnEnable()
        {
            _lookup = null;
            _keyByText = null;
        }

        private void EnsureLookup()
        {
            if (_lookup != null) return;
            _lookup = new Dictionary<string, Entry>(StringComparer.Ordinal);
            _keyByText = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Entry entry in _entries)
                if (entry != null && !string.IsNullOrWhiteSpace(entry.key))
                {
                    _lookup[entry.key] = entry;
                    _keyByText[entry.key] = entry.key;
                    if (!string.IsNullOrEmpty(entry.japanese)) _keyByText[entry.japanese] = entry.key;
                    if (!string.IsNullOrEmpty(entry.english)) _keyByText[entry.english] = entry.key;
                }
        }
    }
}