using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 設定画面のUI制御を行うクラス。
/// BGM・SEの音量調整や言語設定の変更を受け付け、AudioManagerやPlayerPrefsに反映させる役割を持つ。
/// </summary>
public class SettingsController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _seSlider;
    [SerializeField] private TMP_Dropdown _languageDropdown;
    [SerializeField] private Button _closeButton;

    private void Start()
    {
        // 現在の音量設定をUIに反映させる
        if (AudioManager.Instance != null)
        {
            _bgmSlider.value = AudioManager.Instance.GetBGMVolume();
            _seSlider.value = AudioManager.Instance.GetSEVolume();
        }

        _bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        _seSlider.onValueChanged.AddListener(OnSEVolumeChanged);
        _languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        _closeButton.onClick.AddListener(CloseSettings);

        // ゲーム開始時は設定画面を隠しておく
        _settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        _settingsPanel.SetActive(true);
        
        // 他の場所で音量が変更された可能性を考慮し、開くたびにスライダーの値を最新化する
        if (AudioManager.Instance != null)
        {
            _bgmSlider.value = AudioManager.Instance.GetBGMVolume();
            _seSlider.value = AudioManager.Instance.GetSEVolume();
        }
    }

    public void CloseSettings()
    {
        _settingsPanel.SetActive(false);
        PlayerPrefs.Save(); // 設定変更を確実にディスクに書き込む
    }

    private void OnBGMVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(value);
        }
    }

    private void OnSEVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSEVolume(value);
        }
    }

    private void OnLanguageChanged(int index)
    {
        Debug.Log($"Language changed to index: {index}");
        // TODO: 多言語対応の実装時にここを更新する
        PlayerPrefs.SetInt("Language", index);
    }
}
