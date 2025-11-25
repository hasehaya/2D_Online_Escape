using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// ゲーム全体の音響を管理するシングルトンクラス。
/// BGMとSEの再生機能、および音量設定の保存・読み込み（永続化）を担当する。
/// シーン遷移しても破棄されず、常に存在する。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;

    [Header("Volume Settings (0.0 - 1.0)")]
    private float bgmVolume = 1.0f;
    private float seVolume = 1.0f;

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
        bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5f);
        seVolume = PlayerPrefs.GetFloat(SE_VOLUME_KEY, 0.5f);

        ApplyVolume();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
        ApplyVolume();
    }

    public void SetSEVolume(float volume)
    {
        seVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SE_VOLUME_KEY, seVolume);
        ApplyVolume();
    }

    public float GetBGMVolume() => bgmVolume;
    public float GetSEVolume() => seVolume;

    private void ApplyVolume()
    {
        if (bgmSource != null) bgmSource.volume = bgmVolume;
        if (seSource != null) seSource.volume = seVolume;
    }

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null) return;

        // 同じ曲が既に流れている場合は、曲の頭出しを避けるために何もしない
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void PlaySE(AudioClip clip)
    {
        if (seSource == null || clip == null) return;
        seSource.PlayOneShot(clip, seVolume);
    }
}
