/// <summary>
/// ゲーム固有のSE ID。RunconaLib側には依存させず、拡張メソッドで文字列IDへ変換する。
/// 値の追加時は Assets/Specifications/07_Sound/SOUND_SPECS.md も更新すること。
/// </summary>
public enum SESoundType
{
    Correct = 0,
    CorrectBoxOpen = 1,
    CauldronInsert = 2,
    CauldronFail = 3,
    CauldronComplete = 4,
    PlanterGrow = 5,
    PlanterMature = 6,
    PlanterFail = 7,
    PlanterHarvest = 8,
}

/// <summary>
/// ゲーム固有のBGM ID。RunconaLib側には依存させず、拡張メソッドで文字列IDへ変換する。
/// 値の追加時は Assets/Specifications/07_Sound/SOUND_SPECS.md も更新すること。
/// </summary>
public enum BGMSoundType
{
}