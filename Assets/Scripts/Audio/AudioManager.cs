using UnityEngine;

/// <summary>
/// ゲーム全体の音響を管理するシングルトンクラス。
/// BGMとSEの再生機能、および音量設定の保存・読み込み（永続化）を担当する。
/// クリップの選択は AudioDatabase に委譲し、このクラスは再生と音量管理のみを担う。
/// シーン遷移しても破棄されず、常に存在する。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")] [SerializeField]
    private AudioSource _bgmSource;

    [SerializeField] private AudioSource _seSource;

    [Header("Audio Database")] [SerializeField]
    private AudioDatabase _audioDatabase;

    [Header("Volume Settings (0.0 - 1.0)")]
    private float _bgmVolume = 1.0f;

    private float _seVolume = 1.0f;

    private const string BGM_VOLUME_KEY = "BGM_Volume";
    private const string SE_VOLUME_KEY = "SE_Volume";

    private void Awake()
    {
        // シーン遷移してもBGMを途切れさせないため、シングルトンとして保持する
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVolumeSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadVolumeSettings()
    {
        _bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5f);
        _seVolume = PlayerPrefs.GetFloat(SE_VOLUME_KEY, 0.5f);

        ApplyVolume();
    }

    public void SetBGMVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, _bgmVolume);
        ApplyVolume();
    }

    public void SetSEVolume(float volume)
    {
        _seVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SE_VOLUME_KEY, _seVolume);
        ApplyVolume();
    }

    public float GetBGMVolume() => _bgmVolume;
    public float GetSEVolume() => _seVolume;

    private void ApplyVolume()
    {
        if (_bgmSource != null) _bgmSource.volume = _bgmVolume;
        if (_seSource != null) _seSource.volume = _seVolume;
    }

    public void PlayBGM(BGMSoundType type)
    {
        if (_bgmSource == null || _audioDatabase == null) return;
        if (!_audioDatabase.TryGetBGMClip(type, out AudioClip clip)) return;

        // 同じ曲が既に流れている場合は、曲の頭出しを避けるために何もしない
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    public void PlaySE(SESoundType type)
    {
        if (_seSource == null || _audioDatabase == null) return;
        if (!_audioDatabase.TryGetSEClip(type, out AudioClip clip)) return;
        _seSource.PlayOneShot(clip, _seVolume);
    }
}