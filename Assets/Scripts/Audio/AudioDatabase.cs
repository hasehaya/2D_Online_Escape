using System;
using UnityEngine;

/// <summary>
/// SE/BGM の Enum とAudioClip の対応表を保持する ScriptableObject。
/// Assets/Data/AudioDatabase.asset として作成し、AudioManager にアサインする。
/// </summary>
[CreateAssetMenu(fileName = "AudioDatabase", menuName = "Audio/AudioDatabase")]
public class AudioDatabase : ScriptableObject
{
    [Serializable]
    public class SEEntry
    {
        public SESoundType type;
        public AudioClip clip;
    }

    [Serializable]
    public class BGMEntry
    {
        public BGMSoundType type;
        public AudioClip clip;
    }

    [SerializeField] private SEEntry[] _seEntries;
    [SerializeField] private BGMEntry[] _bgmEntries;

    public bool TryGetSEClip(SESoundType type, out AudioClip clip)
    {
        foreach (SEEntry entry in _seEntries)
        {
            if (entry.type == type)
            {
                clip = entry.clip;
                return clip != null;
            }
        }

        clip = null;
        return false;
    }

    public bool TryGetBGMClip(BGMSoundType type, out AudioClip clip)
    {
        foreach (BGMEntry entry in _bgmEntries)
        {
            if (entry.type == type)
            {
                clip = entry.clip;
                return clip != null;
            }
        }

        clip = null;
        return false;
    }
}