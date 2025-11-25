/// <summary>
/// ゲーム内の全てのフラグを管理する列挙型。
/// 新しいフラグを追加する場合はここに追記する。
/// </summary>
public enum FlagType
{
    None = 0,
    
    // テスト用
    TestFlag_A,
    TestFlag_B,

    // ここにゲーム固有のフラグを追加していく
    // 例:
    // Room1_DoorOpen,
    // Room1_SwitchOn,
}
