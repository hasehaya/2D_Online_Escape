using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ADVシステムのダイアログUI表示を管理するコンポーネント。
/// StillNodeで定義されたダイアログを順番に表示し、ユーザーの入力で進行する。
/// </summary>
public class DialogueController : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")] [SerializeField]
    private GameObject _dialoguePanel;

    [SerializeField] private TMP_Text _dialogueText;

    [Header("Settings")] [SerializeField] private float _textSpeed = 0.05f; // 1文字あたりの表示速度(秒)
    [SerializeField] private bool _autoAdvance; // 自動で次のダイアログに進むか

    private DialogueEntry[] _currentDialogues;
    private int _currentDialogueIndex;
    private bool _isTyping;
    private Coroutine _typingCoroutine;
    private StillNode _currentStillNode;

    private void Start()
    {
        HideDialogue();
    }

    /// <summary>
    /// StillNodeのダイアログを開始する
    /// </summary>
    public void StartDialogue(StillNode stillNode)
    {
        _currentStillNode = stillNode;
        _currentDialogues = stillNode.dialogues;
        _currentDialogueIndex = 0;

        if (_currentDialogues != null && _currentDialogues.Length > 0)
        {
            ShowDialogue();
            DisplayCurrentDialogue();
        }
    }

    /// <summary>
    /// 現在のダイアログを表示する
    /// </summary>
    private void DisplayCurrentDialogue()
    {
        if (_currentDialogues == null || _currentDialogueIndex >= _currentDialogues.Length)
        {
            EndDialogue();
            return;
        }

        DialogueEntry entry = _currentDialogues[_currentDialogueIndex];
        string fullText = string.IsNullOrEmpty(entry.CharacterName)
            ? entry.Text
            : $"{entry.CharacterName}\n{entry.Text}";

        // テキストをタイピング表示
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }

        _typingCoroutine = StartCoroutine(TypeText(fullText));
    }

    /// <summary>
    /// テキストを1文字ずつ表示するコルーチン
    /// </summary>
    private IEnumerator TypeText(string text)
    {
        _isTyping = true;
        _dialogueText.text = "";

        foreach (char c in text)
        {
            _dialogueText.text += c;
            yield return new WaitForSeconds(_textSpeed);
        }

        _isTyping = false;

        if (_autoAdvance)
        {
            yield return new WaitForSeconds(1.0f);
            OnNextButtonClicked();
        }
    }

    /// <summary>
    /// 次へボタンがクリックされた時の処理
    /// </summary>
    private void OnNextButtonClicked()
    {
        if (_isTyping)
        {
            // タイピング中の場合は即座に全文表示
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
            }

            DialogueEntry entry = _currentDialogues[_currentDialogueIndex];
            _dialogueText.text = string.IsNullOrEmpty(entry.CharacterName)
                ? entry.Text
                : $"{entry.CharacterName}\n{entry.Text}";
            _isTyping = false;
        }
        else
        {
            // 次のダイアログへ進む
            _currentDialogueIndex++;
            DisplayCurrentDialogue();
        }
    }

    /// <summary>
    /// ダイアログを終了して次のViewableへ遷移
    /// </summary>
    private void EndDialogue()
    {
        HideDialogue();

        if (_currentStillNode != null)
        {
            IViewable nextView = _currentStillNode.GetNextViewable();
            if (nextView != null && ViewController.Instance != null)
            {
                ViewController.Instance.ShowViewable(nextView);
            }
        }
    }

    /// <summary>
    /// ダイアログUIを表示
    /// </summary>
    private void ShowDialogue()
    {
        if (_dialoguePanel != null)
        {
            _dialoguePanel.SetActive(true);
        }
    }

    /// <summary>
    /// ダイアログUIを非表示
    /// </summary>
    private void HideDialogue()
    {
        if (_dialoguePanel != null)
        {
            _dialoguePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Update is called once per frame (スペースキーでも進められるようにする)
    /// </summary>
    private void Update()
    {
        if (_dialoguePanel.activeSelf && Input.GetKeyDown(KeyCode.Space))
        {
            OnNextButtonClicked();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_dialoguePanel.activeSelf)
        {
            OnNextButtonClicked();
        }
    }
}