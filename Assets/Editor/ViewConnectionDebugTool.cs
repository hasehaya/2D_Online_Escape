using System.Collections.Generic;
using Escape.SceneObject.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// ZoomObject と Still 関連の参照を Scene View 上に矢印で表示するデバッグツール。
/// </summary>
[InitializeOnLoad]
public sealed class ViewConnectionDebugTool : EditorWindow
{
    private const string EnabledKey = "ViewConnectionDebugTool.Enabled";
    private const string ZoomEnabledKey = "ViewConnectionDebugTool.ZoomEnabled";
    private const string StillEnabledKey = "ViewConnectionDebugTool.StillEnabled";
    private const double CacheLifetime = 0.5d;

    private static readonly Color ConnectionColor = Color.red;
    private static readonly List<Connection> Connections = new List<Connection>();

    private static double nextCacheUpdate;
    private static bool cacheDirty = true;

    static ViewConnectionDebugTool()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.hierarchyChanged += InvalidateCache;
        Undo.undoRedoPerformed += InvalidateCache;
        EditorSceneManager.sceneOpened += (_, _) => InvalidateCache();
        EditorSceneManager.sceneClosed += _ => InvalidateCache();
    }

    [MenuItem("Tools/View接続デバッグツール")]
    private static void Open()
    {
        GetWindow<ViewConnectionDebugTool>("View接続デバッグ");
    }

    private void OnEnable()
    {
        minSize = new Vector2(320f, 150f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Scene View 接続表示", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "接続元から参照先へ矢印を表示します。参照先が未設定の項目は表示されません。",
            MessageType.Info);

        DrawPreferenceToggle(EnabledKey, "矢印を表示", true);
        using (new EditorGUI.DisabledScope(!EditorPrefs.GetBool(EnabledKey, true)))
        {
            DrawPreferenceToggle(ZoomEnabledKey, "ZoomObject → ViewNode", true);
            DrawPreferenceToggle(StillEnabledKey, "Still 関連", true);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Still 関連は、StillNode を参照するコンポーネントと、");
        EditorGUILayout.LabelField("StillNode から次の ViewNode / StillNode への接続を表示します。");
    }

    private static void DrawPreferenceToggle(string key, string label, bool defaultValue)
    {
        bool current = EditorPrefs.GetBool(key, defaultValue);
        bool updated = EditorGUILayout.ToggleLeft(label, current);

        if (updated == current) return;

        EditorPrefs.SetBool(key, updated);
        InvalidateCache();
    }

    private static void InvalidateCache()
    {
        cacheDirty = true;
        SceneView.RepaintAll();
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!EditorPrefs.GetBool(EnabledKey, true) || Event.current.type != EventType.Repaint) return;

        RefreshCacheIfNeeded();
        Handles.zTest = CompareFunction.Always;

        foreach (Connection connection in Connections)
        {
            if (connection.Source == null || connection.Target == null) continue;
            DrawArrow(sceneView, connection);
        }
    }

    private static void RefreshCacheIfNeeded()
    {
        if (!cacheDirty && EditorApplication.timeSinceStartup < nextCacheUpdate) return;

        cacheDirty = false;
        nextCacheUpdate = EditorApplication.timeSinceStartup + CacheLifetime;
        Connections.Clear();

        if (EditorPrefs.GetBool(ZoomEnabledKey, true)) AddZoomConnections();
        if (EditorPrefs.GetBool(StillEnabledKey, true)) AddStillConnections();
    }

    private static void AddZoomConnections()
    {
        foreach (ZoomObject zoomObject in UnityEngine.Object.FindObjectsByType<ZoomObject>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!IsInLoadedScene(zoomObject)) continue;

            SerializedProperty property = new SerializedObject(zoomObject).FindProperty("_zoomViewNode");
            if (property?.objectReferenceValue is ViewNode target)
            {
                Connections.Add(new Connection(zoomObject.transform, target.transform));
            }
        }
    }

    private static void AddStillConnections()
    {
        foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (behaviour == null || !IsInLoadedScene(behaviour)) continue;

            SerializedProperty property = new SerializedObject(behaviour).GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyType != SerializedPropertyType.ObjectReference ||
                    !(property.objectReferenceValue is StillNode target)) continue;

                Connections.Add(new Connection(
                    behaviour.transform,
                    target.transform));
            }
        }

        foreach (StillNode stillNode in UnityEngine.Object.FindObjectsByType<StillNode>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!IsInLoadedScene(stillNode) || stillNode.nextViewNode == null) continue;

            Connections.Add(new Connection(
                stillNode.transform,
                stillNode.nextViewNode.transform));
        }
    }

    private static bool IsInLoadedScene(Component component)
    {
        return component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded;
    }

    private static void DrawArrow(SceneView sceneView, Connection connection)
    {
        Vector3 start = GetCenter(connection.Source);
        Vector3 end = GetCenter(connection.Target);
        Vector3 direction = end - start;
        if (direction.sqrMagnitude < 0.0001f) return;

        Handles.color = ConnectionColor;
        Handles.DrawAAPolyLine(4f, start, end);

        float arrowSize = Mathf.Min(HandleUtility.GetHandleSize(end) * 0.18f, direction.magnitude * 0.25f);
        Vector3 forward = direction.normalized;
        Vector3 viewNormal = sceneView.camera != null ? sceneView.camera.transform.forward : Vector3.forward;
        Vector3 side = Vector3.Cross(viewNormal, forward).normalized;
        if (side.sqrMagnitude < 0.001f) side = Vector3.up;

        Vector3 arrowBase = end - forward * arrowSize;
        Handles.DrawAAConvexPolygon(
            end,
            arrowBase + side * arrowSize * 0.45f,
            arrowBase - side * arrowSize * 0.45f);

    }

    private static Vector3 GetCenter(Transform target)
    {
        if (target is RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f;
        }

        return target.position;
    }

    private readonly struct Connection
    {
        public Connection(Transform source, Transform target)
        {
            Source = source;
            Target = target;
        }

        public Transform Source { get; }
        public Transform Target { get; }
    }
}
