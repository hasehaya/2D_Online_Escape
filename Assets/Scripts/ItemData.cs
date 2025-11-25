using UnityEngine;

/// <summary>
/// アイテムの静的なデータを定義するScriptableObject。
/// アイテムID、名前、アイコン画像、説明文などのプロパティを保持するデータコンテナ。
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "EscapeGame/Item")]
public class ItemData : ScriptableObject
{
    public string id;
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
}
