using UnityEngine;

/// <summary>
/// ADVスチル画面を定義するコンポーネント。
/// シーン内のGameObjectにアタッチして使用する。
/// スチル画像、会話データ、次の遷移先(ViewNodeまたはStillNode)を保持する。
/// </summary>
public class StillNode : MonoBehaviour, IViewable
{
    [Header("Still Settings")] [Tooltip("このStillの識別名（デバッグ用）")]
    public string stillName;

    [Header("Dialogue")] [Tooltip("このスチルで再生する会話データ")]
    public DialogueEntry[] dialogues;

    [Header("Navigation")] [Tooltip("会話終了後に遷移する次のViewNode")]
    public ViewNode nextViewNode;

    [Tooltip("会話終了後に遷移する次のStillNode")] public StillNode nextStillNode;

    /// <summary>
    /// この視点に入った時の処理
    /// </summary>
    public void OnEnter()
    {
        // ダイアログを開始
        DialogueController dialogueController = FindObjectOfType<DialogueController>();
        if (dialogueController != null && dialogues != null && dialogues.Length > 0)
        {
            dialogueController.StartDialogue(this);
        }
    }

    /// <summary>
    /// このViewableのTransform（カメラ位置設定用）
    /// </summary>
    public Transform GetTransform()
    {
        return transform;
    }

    /// <summary>
    /// 次に遷移するIViewableを取得する
    /// ViewNodeとStillNodeの両方をチェックして、設定されている方を返す
    /// </summary>
    public IViewable GetNextViewable()
    {
        if (nextViewNode != null)
        {
            return nextViewNode;
        }

        if (nextStillNode != null)
        {
            return nextStillNode;
        }

        return null;
    }
}