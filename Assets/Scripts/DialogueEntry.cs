using System;
using UnityEngine;

/// <summary>
/// ADVシステムで使用する1つのダイアログ(セリフ)のデータ。
/// キャラクター名と発言内容を保持する。
/// </summary>
[Serializable]
public class DialogueEntry
{
    [Tooltip("発言するキャラクター名(空欄の場合はナレーション)")] [SerializeField]
    private string _characterName;

    [Tooltip("発言内容")] [TextArea(3, 10)] [SerializeField]
    private string _text;

    public string CharacterName => _characterName;
    public string Text => _text;
}