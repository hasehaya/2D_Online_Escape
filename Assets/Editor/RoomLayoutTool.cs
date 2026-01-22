using UnityEngine;
using UnityEditor;

public class RoomLayoutTool
{
    // 固定の設定値
    private const float Margin = 150.0f;
    private const float HorizontalSpacing = 1920.0f + Margin; // 横の間隔 (View X)
    private const float VerticalSpacing = 1080.0f + Margin;   // 縦の間隔 (Room Y)

    [MenuItem("Tools/Room Layout Tool")]
    public static void AutoAlign()
    {
        // 1. "Map" オブジェクトを検索
        GameObject mapRoot = GameObject.Find("Map");

        // 見つからない場合、Canvasの中も探す
        if (mapRoot == null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                Transform mapTrans = canvas.transform.Find("Map");
                if (mapTrans != null) mapRoot = mapTrans.gameObject;
            }
        }

        if (mapRoot == null)
        {
            EditorUtility.DisplayDialog("エラー", "シーン内に 'Map' が見つかりません。", "OK");
            return;
        }

        // Transform変更のUndo登録
        Undo.RecordObjects(mapRoot.GetComponentsInChildren<Transform>(true), "Auto Align Rooms");

        float currentRoomY = 0f;
        int addedCount = 0;

        // Map直下の子(Room)をループ
        foreach (Transform room in mapRoot.transform)
        {
            room.localPosition = new Vector3(0, currentRoomY, 0);

            float currentViewX = 0f;
            
            // Roomの中にある子(View)をループ
            foreach (Transform child in room)
            {
                // ★変更点: ViewNodeがついてなければ追加する
                ViewNode node = child.GetComponent<ViewNode>();
                if (node == null)
                {
                    // Undo対応つきでコンポーネントを追加
                    node = Undo.AddComponent<ViewNode>(child.gameObject);
                    addedCount++;
                }

                // 整列処理
                child.localPosition = new Vector3(currentViewX, 0, 0);
                currentViewX += HorizontalSpacing;
            }

            // 次の部屋のためにY座標を下げる
            currentRoomY -= VerticalSpacing;
        }

        string msg = $"'{mapRoot.name}' 配下を整列しました。";
        if (addedCount > 0) msg += $" ({addedCount}個のViewNodeを自動追加)";
        
        Debug.Log($"<color=cyan><b>[Room Layout Tool]</b></color> {msg}");
    }
}