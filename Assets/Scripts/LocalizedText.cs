using TMPro;
using UnityEngine;

/// <summary>
/// LocalizationManagerのキーに応じてTMPテキストを更新する。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string _key;

    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }
    }

    private void HandleLanguageChanged(int _)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (_text == null || string.IsNullOrEmpty(_key))
        {
            return;
        }

        if (LocalizationManager.Instance != null)
        {
            _text.text = LocalizationManager.Instance.Get(_key);
        }
    }
}