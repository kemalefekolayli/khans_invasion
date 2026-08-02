#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIPolygonHotspot))]
public class UIPolygonHotspotEditor : Editor
{
    private UIPolygonHotspot hotspot;
    private RectTransform rt;

    // Tool options
    private const float HandleSize = 6f;
    private const float PickRadius = 12f;

    private void OnEnable()
    {
        hotspot = (UIPolygonHotspot)target;
        rt = hotspot.transform as RectTransform;
        if (hotspot.points == null) hotspot.points = new List<Vector2>();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Scene Tool", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Scene view controls:\n" +
            "• SHIFT + Left Click: Add point\n" +
            "• CTRL  + Left Click: Remove nearest point\n" +
            "• ALT   + Left Drag: orbit/pan (Unity default)\n" +
            "Tips: Select the hotspot object, then click on the image in Scene view.",
            MessageType.Info
        );

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clear Points"))
            {
                Undo.RecordObject(hotspot, "Clear Hotspot Points");
                hotspot.points.Clear();
                EditorUtility.SetDirty(hotspot);
            }

            if (GUILayout.Button("Close Polygon (Duplicate First)"))
            {
                // Optional helper: some people like seeing last->first closure as explicit point
                if (hotspot.points.Count >= 3)
                {
                    Undo.RecordObject(hotspot, "Close Polygon");
                    hotspot.points.Add(hotspot.points[0]);
                    EditorUtility.SetDirty(hotspot);
                }
            }
        }
    }

    private void OnSceneGUI(SceneView sv)
    {
        if (hotspot == null || rt == null) return;
        if (Selection.activeGameObject != hotspot.gameObject) return;

        DrawPolygon();

        Event e = Event.current;
        if (e == null) return;

        // Don't fight Unity's alt controls (orbit/pan)
        if (e.alt) return;

        // SHIFT + LMB add point
        if (e.type == EventType.MouseDown && e.button == 0 && e.shift)
        {
            if (TryGetLocalPointFromMouse(e.mousePosition, out Vector2 local))
            {
                Undo.RecordObject(hotspot, "Add Hotspot Point");
                hotspot.points.Add(local);
                EditorUtility.SetDirty(hotspot);
                e.Use();
            }
        }

        // CTRL + LMB remove nearest point
        if (e.type == EventType.MouseDown && e.button == 0 && e.control)
        {
            if (hotspot.points.Count > 0 && TryGetLocalPointFromMouse(e.mousePosition, out Vector2 local))
            {
                int idx = FindNearestPointIndex(local);
                if (idx != -1)
                {
                    Undo.RecordObject(hotspot, "Remove Hotspot Point");
                    hotspot.points.RemoveAt(idx);
                    EditorUtility.SetDirty(hotspot);
                    e.Use();
                }
            }
        }
    }

    private bool TryGetLocalPointFromMouse(Vector2 mousePosition, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;

        // Scene mousePosition is GUI space (Y inverted). Convert to screen point.
        Camera cam = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.camera : null;
        if (cam == null) return false;

        Vector2 screen = HandleUtility.GUIPointToScreenPixelCoordinate(mousePosition);

        // For Screen Space - Overlay, pass null camera. For others, pass cam.
        Camera eventCam = null;

        // Heuristic: if canvas is ScreenSpaceCamera/WorldSpace, try to find that camera.
        Canvas canvas = hotspot.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            // In ScreenSpaceCamera, use the canvas worldCamera if set, else fallback to scene cam
            eventCam = canvas.worldCamera != null ? canvas.worldCamera : cam;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, eventCam, out localPoint);
    }

    private void DrawPolygon()
    {
        if (hotspot.points == null) return;

        // Draw points + lines in world space
        Handles.color = Color.cyan;

        for (int i = 0; i < hotspot.points.Count; i++)
        {
            Vector3 wp = rt.TransformPoint(hotspot.points[i]);
            float size = HandleUtility.GetHandleSize(wp) * 0.02f;

            Handles.DotHandleCap(0, wp, Quaternion.identity, size, EventType.Repaint);

            if (i > 0)
            {
                Vector3 prev = rt.TransformPoint(hotspot.points[i - 1]);
                Handles.DrawLine(prev, wp);
            }
        }

        // close shape preview (last -> first)
        if (hotspot.points.Count >= 3)
        {
            Vector3 first = rt.TransformPoint(hotspot.points[0]);
            Vector3 last = rt.TransformPoint(hotspot.points[hotspot.points.Count - 1]);
            Handles.DrawDottedLine(last, first, 4f);
        }
    }

    private int FindNearestPointIndex(Vector2 local)
    {
        int best = -1;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hotspot.points.Count; i++)
        {
            float d = Vector2.Distance(local, hotspot.points[i]);
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }

        // Threshold in local units: scale roughly with rect size for usability
        float threshold = Mathf.Max(rt.rect.width, rt.rect.height) * 0.02f;
        if (bestDist > threshold) return -1;

        return best;
    }
}
#endif
