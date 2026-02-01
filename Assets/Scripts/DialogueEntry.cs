using System;
using UnityEngine;

/// <summary>
/// ADVシステムで使用する1つのダイアログ(セリフ)のデータ。
/// キャラクター名と発言内容を保持する。
/// </summary>
[Serializable]
public enum DialogueCharacter
{
    Narration,
    Elias,
    Noel,
    Iris
}

[Serializable]
public class DialogueEntry
{
    [Tooltip("発言するキャラクター(ナレーションを含む)")] [SerializeField]
    private DialogueCharacter _character = DialogueCharacter.Narration;

    [Tooltip("発言内容")] [TextArea(3, 10)] [SerializeField]
    private string _text;

    public DialogueCharacter Character => _character;
    public string CharacterName => _character == DialogueCharacter.Narration ? "" : _character.ToString();
    public string Text => _text;
}