using UnityEngine;

namespace RunconaLib.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const float DefaultBGMVolume = 0.5f;
        private const float DefaultSEVolume = 0.5f;
        private const string AudioDatabaseResourcePath = "AudioDatabase";

        private const string BGMVolumeKey = "RunconaLib.Audio.BGMVolume";
        private const string SEVolumeKey = "RunconaLib.Audio.SEVolume";
        private AudioSource _bgmSource;
        private AudioSource _seSource;
        private AudioDatabase _audioDatabase;
        private float _bgmVolume;
        private float _seVolume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            if (Instance == null)
                new GameObject(nameof(AudioManager)).AddComponent<AudioManager>();
        }

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
            _bgmSource = CreateSource(true);
            _seSource = CreateSource(false);
            _audioDatabase = Resources.Load<AudioDatabase>(AudioDatabaseResourcePath);
            if (_audioDatabase == null)
                Debug.LogWarning(
                    $"{nameof(AudioManager)}: Resources/{AudioDatabaseResourcePath}.asset が見つかりません。音声を再生するには AudioDatabase を配置してください。",
                    this);

            _bgmVolume = PlayerPrefs.GetFloat(BGMVolumeKey, DefaultBGMVolume);
            _seVolume = PlayerPrefs.GetFloat(SEVolumeKey, DefaultSEVolume);
            ApplyVolume();
        }

        private AudioSource CreateSource(bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            return source;
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

        private void ApplyVolume()
        {
            if (_bgmSource != null) _bgmSource.volume = _bgmVolume;
            if (_seSource != null) _seSource.volume = _seVolume;
        }
    }
}