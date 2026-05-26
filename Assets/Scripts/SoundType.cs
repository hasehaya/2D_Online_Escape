/// <summary>
/// SE の種類を表すEnum。AudioDatabase でクリップとの対応を定義する。
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
/// BGM の種類を表すEnum。AudioDatabase でクリップとの対応を定義する。
/// 値の追加時は Assets/Specifications/07_Sound/SOUND_SPECS.md も更新すること。
/// </summary>
public enum BGMSoundType
{
}