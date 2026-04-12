using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public class RoomLayoutTool
{
    private const float Margin = 150.0f;
    private const float HorizontalSpacing = 1920.0f + Margin;
    private const float VerticalSpacing = 1080.0f + Margin;
    private const int MaxColumns = 4; // 4列で折り返し

    private static GameObject cachedMapRoot;
    private static double nextCheckTime = 0;
    private static GUIStyle labelStyle;

    static RoomLayoutTool()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

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

        // Map直下の子(Room)をループ
        foreach (Transform room in mapRoot.transform)
        {
            room.localPosition = new Vector3(0, currentRoomY, 0);

            int childIndex = 0;
            int rowsInThisRoom = 1;

            foreach (Transform child in room)
            {
                if (child.GetComponent<ViewNode>() == null && child.GetComponent<StillNode>() == null)
                {
                    Undo.AddComponent<ViewNode>(child.gameObject);
                    viewNodeAddedCount++;
                }

                int column = childIndex % MaxColumns;
                int row = childIndex / MaxColumns;

                float currentViewX = column * HorizontalSpacing;
                float currentViewY = -row * VerticalSpacing;

                child.localPosition = new Vector3(currentViewX, currentViewY, 0);

                if (row + 1 > rowsInThisRoom) rowsInThisRoom = row + 1;
                childIndex++;
            }

            currentRoomY -= VerticalSpacing * rowsInThisRoom;
        }

        cachedMapRoot = mapRoot;
        Debug.Log($"<color=cyan><b>[Room Layout Tool]</b></color> '{mapRoot.name}' を整列しました。");
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (Event.current.type != EventType.Repaint) return;

        if (cachedMapRoot == null && EditorApplication.timeSinceStartup > nextCheckTime)
        {
            cachedMapRoot = GameObject.Find("Map") ?? GameObject.Find("Canvas")?.transform.Find("Map")?.gameObject;
            nextCheckTime = EditorApplication.timeSinceStartup + 2.0;
        }

        if (cachedMapRoot != null) DrawBounds(cachedMapRoot);
    }

    private static void DrawBounds(GameObject mapRoot)
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(mapRoot.transform.position, Vector3.zero);

        foreach (Transform room in mapRoot.transform)
        {
            foreach (Transform child in room)
            {
                RectTransform rect = child.GetComponent<RectTransform>();
                if (rect != null)
                {
                    Vector3[] corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    foreach (Vector3 corner in corners)
                    {
                        if (!hasBounds)
                        {
                            bounds = new Bounds(corner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(corner);
                        }
                    }
                }
                else
                {
                    if (!hasBounds)
                    {
                        bounds = new Bounds(child.position, Vector3.zero);
                        hasBounds = true;
                    }

                    bounds.Encapsulate(new Bounds(child.position, new Vector3(HorizontalSpacing, VerticalSpacing, 0)));
                }
            }
        }

        if (hasBounds)
        {
            bounds.Expand(100f);
            Vector3 ext = bounds.extents;
            Vector3 center = bounds.center;

            Vector3[] corners = new Vector3[4];
            corners[0] = center + new Vector3(-ext.x, -ext.y, 0);
            corners[1] = center + new Vector3(-ext.x, ext.y, 0);
            corners[2] = center + new Vector3(ext.x, ext.y, 0);
            corners[3] = center + new Vector3(ext.x, -ext.y, 0);

            Handles.zTest = CompareFunction.Always; // 他のオブジェクトに隠れないように
            Handles.DrawSolidRectangleWithOutline(corners, Color.clear, new Color(0f, 1f, 0f, 1f));

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle();
                labelStyle.normal.textColor = Color.green;
                labelStyle.fontSize = 20;
                labelStyle.fontStyle = FontStyle.Bold;
            }
        }
    }
}