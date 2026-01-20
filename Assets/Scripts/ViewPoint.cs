using UnityEngine;

/// <summary>
/// ゲーム内の1つの「視点（View）」を定義するコンポーネント。
/// シーン内のGameObjectにアタッチして使用する。
/// 左右移動時の遷移先（隣接するView）の情報を保持する。
/// Instantiateせずにインスタンスを保持するため、内部オブジェクトの状態が維持される。
/// </summary>
public class ViewPoint : MonoBehaviour
{
    [Header("View Settings")]
    [Tooltip("このViewの識別名（デバッグ用）")]
    public string viewName;

    [Header("Navigation")]
    [Tooltip("左を向いたときに遷移するView")]
    public ViewPoint leftView;

    [Tooltip("右を向いたときに遷移するView")]
    public ViewPoint rightView;

    [Header("Zoom Settings")]
    [Tooltip("これが拡大（ズーム）画面かどうか。trueの場合、戻るボタンが表示される")]
    public bool isZoomView;
}
