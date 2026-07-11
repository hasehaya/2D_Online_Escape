using UnityEngine;

namespace RunconaLib.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _seSource;
        [SerializeField] private AudioDatabase _audioDatabase;
        [SerializeField, Range(0f, 1f)] private float _defaultBGMVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _defaultSEVolume = 0.5f;

        private const string BGMVolumeKey = "RunconaLib.Audio.BGMVolume";
        private const string SEVolumeKey = "RunconaLib.Audio.SEVolume";
        private const string LegacyBGMVolumeKey = "BGM_Volume";
        private const string LegacySEVolumeKey = "SE_Volume";
        private float _bgmVolume;
        private float _seVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            _bgmVolume = LoadVolume(BGMVolumeKey, LegacyBGMVolumeKey, _defaultBGMVolume);
            _seVolume = LoadVolume(SEVolumeKey, LegacySEVolumeKey, _defaultSEVolume);
            ApplyVolume();
        }

        public void SetBGMVolume(float volume)
        {
            _bgmVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(BGMVolumeKey, _bgmVolume);
            ApplyVolume();
        }

        public void SetSEVolume(float volume)
        {
            _seVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SEVolumeKey, _seVolume);
            ApplyVolume();
        }

        public float GetBGMVolume() => _bgmVolume;
        public float GetSEVolume() => _seVolume;

        public bool PlayBGM(string id)
        {
            if (_bgmSource == null || _audioDatabase == null ||
                !_audioDatabase.TryGetBGMClip(id, out AudioClip clip)) return false;
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return true;
            _bgmSource.clip = clip;
            _bgmSource.Play();
            return true;
        }

        public bool PlaySE(string id)
        {
            if (_seSource == null || _audioDatabase == null ||
                !_audioDatabase.TryGetSEClip(id, out AudioClip clip)) return false;
            _seSource.PlayOneShot(clip);
            return true;
        }

        public void StopBGM() => _bgmSource?.Stop();

        private static float LoadVolume(string key, string legacyKey, float defaultValue)
        {
            if (PlayerPrefs.HasKey(key)) return PlayerPrefs.GetFloat(key, defaultValue);
            float value = PlayerPrefs.GetFloat(legacyKey, defaultValue);
            PlayerPrefs.SetFloat(key, value);
            return value;
        }

        private void ApplyVolume()
        {
            if (_bgmSource != null) _bgmSource.volume = _bgmVolume;
            if (_seSource != null) _seSource.volume = _seVolume;
        }
    }
}