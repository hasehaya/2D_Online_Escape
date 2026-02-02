using UnityEditor;
using UnityEngine;

public class RoomLayoutTool
{
    // 固定の設定値
    private const float Margin = 150.0f;
    private const float HorizontalSpacing = 1920.0f + Margin; // 横の間隔 (View X)
    private const float VerticalSpacing = 1080.0f + Margin; // 縦の間隔 (Room Y)

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
        int viewNodeAddedCount = 0;
        int stillNodeAddedCount = 0;

        // Map直下の子(Room)をループ
        foreach (Transform room in mapRoot.transform)
        {
            room.localPosition = new Vector3(0, currentRoomY, 0);

            float currentViewX = 0f;

            // Roomの中にある子(View/Still)をループ
            foreach (Transform child in room)
            {
                // ViewNodeまたはStillNodeがついてなければ追加する
                ViewNode viewNode = child.GetComponent<ViewNode>();
                StillNode stillNode = child.GetComponent<StillNode>();

                if (viewNode == null && stillNode == null)
                {
                    // 両方ついていない場合はViewNodeを追加
                    viewNode = Undo.AddComponent<ViewNode>(child.gameObject);
                    viewNodeAddedCount++;
                }
                else if (stillNode != null && viewNode == null)
                {
                    // StillNodeだけある場合は何もしない（正常）
                }
                else if (viewNode != null && stillNode == null)
                {
                    // ViewNodeだけある場合は何もしない（正常）
                }

                // 整列処理
                child.localPosition = new Vector3(currentViewX, 0, 0);
                currentViewX += HorizontalSpacing;
            }

            // 次の部屋のためにY座標を下げる
            currentRoomY -= VerticalSpacing;
        }

        string msg = $"'{mapRoot.name}' 配下を整列しました。";
        if (viewNodeAddedCount > 0) msg += $" ({viewNodeAddedCount}個のViewNodeを自動追加)";
        if (stillNodeAddedCount > 0) msg += $" ({stillNodeAddedCount}個のStillNodeを自動追加)";

        Debug.Log($"<color=cyan><b>[Room Layout Tool]</b></color> {msg}");
    }
}