using System;
using UnityEngine;

/// <summary>
/// アイテムの静的なデータを定義するシンプルなデータクラス。
/// ItemDatabase の List にインラインで保持する。
/// </summary>
[Serializable]
public class ItemData
{
    public ItemType itemType;
    public Sprite icon;
}