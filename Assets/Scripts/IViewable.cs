using UnityEngine;

/// <summary>
/// ViewNodeとStillNodeを統一的に扱うためのインターフェース。
/// ViewManagerで遷移可能な全ての「視点」型に実装される。
/// </summary>
public interface IViewable
{
    /// <summary>
    /// この視点に入った時の処理
    /// </summary>
    void OnEnter();

    /// <summary>
    /// この視点から出る時の処理
    /// </summary>
    void OnExit();

    /// <summary>
    /// このViewableのTransform（カメラ位置設定用）
    /// </summary>
    Transform GetTransform();
}