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
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private Button closeButton;

    private void Start()
    {
        // 現在の音量設定をUIに反映させる
        if (AudioManager.Instance != null)
        {
            bgmSlider.value = AudioManager.Instance.GetBGMVolume();
            seSlider.value = AudioManager.Instance.GetSEVolume();
        }

        bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        seSlider.onValueChanged.AddListener(OnSEVolumeChanged);
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        closeButton.onClick.AddListener(CloseSettings);

        // ゲーム開始時は設定画面を隠しておく
        settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        
        // 他の場所で音量が変更された可能性を考慮し、開くたびにスライダーの値を最新化する
        if (AudioManager.Instance != null)
        {
            bgmSlider.value = AudioManager.Instance.GetBGMVolume();
            seSlider.value = AudioManager.Instance.GetSEVolume();
        }
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
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
