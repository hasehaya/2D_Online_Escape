/// <summary>
/// ゲーム内の全てのフラグを管理する列挙型。
/// 新しいフラグを追加する場合はここに追記する。
/// </summary>
public enum FlagType
{
    None = 0,

    // Wake - レーザーギミック
    Wake_LaserTarget1,
    Wake_LaserTarget2,
    Wake_LaserTarget3,
    Wake_LaserCompleted,
    Prepare_PianoCompleted,

    // Dungeon - ライツアウトパズル
    Dungeon_LightsOutPuzzleCompleted,
}
