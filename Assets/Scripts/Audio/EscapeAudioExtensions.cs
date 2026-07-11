using RunconaLib.Audio;

/// <summary>ゲーム固有のSoundTypeをRunconaLibの文字列IDへ変換する境界。</summary>
public static class EscapeAudioExtensions
{
    public static bool PlaySE(this AudioManager manager, SESoundType type) =>
        manager != null && manager.PlaySE(type.ToString());

    public static bool PlayBGM(this AudioManager manager, BGMSoundType type) =>
        manager != null && manager.PlayBGM(type.ToString());
}