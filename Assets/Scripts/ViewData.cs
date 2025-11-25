using UnityEngine;

/// <summary>
/// ゲーム内の1つの「視点（View）」を定義するデータクラス。
/// 表示するPrefabや、左右移動時の遷移先（隣接するView）の情報を保持する。
/// ScriptableObjectとしてアセット保存され、シーン間で共有可能。
/// </summary>
[CreateAssetMenu(fileName = "New View", menuName = "EscapeGame/ViewData")]
public class ViewData : ScriptableObject
{
    [Header("View Settings")]
    [Tooltip("このViewの識別名（デバッグ用）")]
    public string viewName;

    [Tooltip("このViewで表示するPrefab（背景やオブジェクトを含む）")]
    public GameObject viewPrefab;

    [Header("Navigation")]
    [Tooltip("左を向いたときに遷移するView")]
    public ViewData leftView;

    [Tooltip("右を向いたときに遷移するView")]
    public ViewData rightView;

    [Header("Zoom Settings")]
    [Tooltip("これが拡大（ズーム）画面かどうか。trueの場合、戻るボタンが表示される")]
    public bool isZoomView;
}
