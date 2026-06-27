/// <summary>
/// アイテム識別用のenum。
/// Inspector上の順序ずれを防ぐため、全要素に明示的な数値を付ける。
/// </summary>
public enum ItemType
{
    None = 0,

    Leaf1 = 1,
    Leaf2 = 2,
    Leaf3 = 3,
    PaperScrap = 4,
    Nutrient = 5,
    Cd = 6,
    WateringCan = 7,
    HealPotionEmpty = 8,
    HealPotionFilled = 9,
    CurePotionEmpty = 10,
    CurePotionFilled = 11,
    Pendant = 12,
    Match = 13,
    MagicSack = 14,

    KeyEliasPrepareWoodBox = 101,
}