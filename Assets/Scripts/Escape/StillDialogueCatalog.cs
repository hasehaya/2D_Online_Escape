using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Escape/Still/Dialogue Catalog", fileName = "StillDialogueCatalog")]
public sealed class StillDialogueCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Still
    {
        public string id;
        public DialogueEntry[] dialogues;
    }

    [SerializeField] private List<Still> _stills = new List<Still>();
    public IReadOnlyList<Still> Stills => _stills;

    public bool TryGet(string id, out DialogueEntry[] dialogues)
    {
        Still still = _stills.Find(x => x.id == id);
        dialogues = still?.dialogues;
        return dialogues != null;
    }

    public void Replace(IEnumerable<Still> stills) => _stills = new List<Still>(stills);
}