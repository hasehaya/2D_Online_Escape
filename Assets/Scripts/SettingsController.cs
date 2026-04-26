using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 設定画面のUI制御を行うクラス。
/// BGM・SEの音量調整や言語設定の変更を受け付け、AudioManagerやPlayerPrefsに反映させる役割を持つ。
/// </summary>
public class SettingsController : MonoBehaviour
{
    private const string LanguageKey = "Language";

    [Header("UI References")] [SerializeField]
    private GameObject _settingsPanel;

    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _seSlider;
    [SerializeField] private TMP_Dropdown _languageDropdown;
    [SerializeField] private Button _closeButton;

    private void Start()
    {
        if (_settingsPanel == null)
        {
            _settingsPanel = gameObject;
        }

        EnsureRuntimeControls();

        // 現在の音量設定をUIに反映させる
        if (AudioManager.Instance != null && _bgmSlider != null && _seSlider != null)
        {
            _bgmSlider.value = AudioManager.Instance.GetBGMVolume();
            _seSlider.value = AudioManager.Instance.GetSEVolume();
        }

        if (_bgmSlider != null)
        {
            _bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (_seSlider != null)
        {
            _seSlider.onValueChanged.AddListener(OnSEVolumeChanged);
        }

        if (_languageDropdown != null)
        {
            _languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(CloseSettings);
        }

        int currentLanguage = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.CurrentLanguageIndex
            : PlayerPrefs.GetInt(LanguageKey, 0);
        if (_languageDropdown != null)
        {
            _languageDropdown.SetValueWithoutNotify(currentLanguage);
        }

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

    private void EnsureRuntimeControls()
    {
        if (_settingsPanel == null)
        {
            return;
        }

        RectTransform panelRect = _settingsPanel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            panelRect = _settingsPanel.AddComponent<RectTransform>();
        }

        if (_settingsPanel.GetComponent<Image>() == null)
        {
            Image image = _settingsPanel.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.85f);
        }

        panelRect.sizeDelta = new Vector2(640f, 420f);

        if (_bgmSlider == null)
        {
            _bgmSlider = CreateSlider("BGMSlider", panelRect, new Vector2(0f, 100f));
        }

        if (_seSlider == null)
        {
            _seSlider = CreateSlider("SESlider", panelRect, new Vector2(0f, 20f));
        }

        if (_languageDropdown == null)
        {
            _languageDropdown = CreateLanguageDropdown("LanguageDropdown", panelRect, new Vector2(0f, -60f));
        }

        if (_closeButton == null)
        {
            _closeButton = CreateCloseButton("CloseButton", panelRect, new Vector2(0f, -150f));
        }
    }

    private Slider CreateSlider(string name, RectTransform parent, Vector2 anchoredPosition)
    {
        GameObject sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderObject.name = name;
        sliderObject.transform.SetParent(parent, false);

        RectTransform rect = sliderObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360f, 40f);
        rect.anchoredPosition = anchoredPosition;

        return sliderObject.GetComponent<Slider>();
    }

    private TMP_Dropdown CreateLanguageDropdown(string name, RectTransform parent, Vector2 anchoredPosition)
    {
        GameObject dropdownObject = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
        dropdownObject.name = name;
        dropdownObject.transform.SetParent(parent, false);

        RectTransform rect = dropdownObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360f, 50f);
        rect.anchoredPosition = anchoredPosition;

        TMP_Dropdown dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
        dropdown.options = new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("Japanese"),
            new TMP_Dropdown.OptionData("English")
        };

        return dropdown;
    }

    private Button CreateCloseButton(string name, RectTransform parent, Vector2 anchoredPosition)
    {
        GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
        buttonObject.name = name;
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(220f, 70f);
        rect.anchoredPosition = anchoredPosition;

        Text label = buttonObject.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.text = "Close";
        }

        return buttonObject.GetComponent<Button>();
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
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.SetLanguage(index);
        }
        else
        {
            PlayerPrefs.SetInt(LanguageKey, index);
            PlayerPrefs.Save();
        }
    }
}