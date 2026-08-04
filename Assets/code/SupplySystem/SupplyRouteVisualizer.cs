using System.Collections.Generic;
using UnityEngine;

/// <summary>Rendering-only view of the selected army's supply route.</summary>
public class SupplyRouteVisualizer : MonoBehaviour
{
    [Header("Active Expedition")]
    [SerializeField] private Color activeColor = new(0.2f, 0.9f, 0.3f, 0.9f);
    [SerializeField, Min(0.01f)] private float activeWidth = 0.1f;
    [SerializeField] private Color repeatColor = new(0.75f, 1f, 0.3f, 1f);
    [SerializeField, Min(0.01f)] private float repeatWidth = 0.07f;
    [SerializeField, Min(0.01f)] private float repeatOffset = 0.08f;
    [Header("Pending Trail")]
    [SerializeField] private Color pendingColor = new(0.2f, 0.9f, 0.3f, 0.35f);
    [SerializeField, Min(0.01f)] private float pendingWidth = 0.06f;
    [Header("Rendering")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int activeSortingOrder = 20;
    [SerializeField] private int repeatSortingOrder = 21;
    [SerializeField] private int pendingSortingOrder = 22;
    [SerializeField] private float routeZOffset = -0.1f;
    [SerializeField] private Material routeMaterial;
    private readonly List<LineRenderer> activeLines = new();
    private readonly List<LineRenderer> repeatLines = new();
    private LineRenderer pendingLine;
    private Material runtimeMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindFirstObjectByType<SupplyRouteVisualizer>() != null) return;
        GameObject host = new(nameof(SupplyRouteVisualizer));
        DontDestroyOnLoad(host);
        host.AddComponent<SupplyRouteVisualizer>();
    }

    private void Awake() => pendingLine = CreateLine("Pending Supply Trail");
    private void Update() => Refresh();
    private void OnDestroy() { if (runtimeMaterial != null) Destroy(runtimeMaterial); }

    private void Refresh()
    {
        SupplyRouteTracker tracker = SupplyRouteTracker.Instance;
        NationModel nation = PlayerNation.Instance?.currentNation;
        Army army = GeneralSelectionManager.Instance?.SelectedGeneral?.GetComponent<General>()?.CommandedArmy;
        if (tracker == null || nation == null || army == null || !army.IsPlayerArmy || (army.OwnerNation != null && army.OwnerNation != nation))
        {
            pendingLine.positionCount = 0;
            SetVisible(activeLines, 0);
            SetVisible(repeatLines, 0);
            return;
        }
        SupplyRouteTracker.ArmyRouteState state = tracker.GetArmyState(army);
        DrawPending(state.PendingRoute);
        DrawNetwork(state.ActiveEdges);
    }

    private void DrawPending(List<ProvinceModel> route)
    {
        if (route == null || route.Count < 2) { pendingLine.positionCount = 0; return; }
        Configure(pendingLine, pendingColor, pendingWidth, pendingSortingOrder);
        pendingLine.positionCount = route.Count;
        for (int i = 0; i < route.Count; i++) pendingLine.SetPosition(i, Position(route[i]));
    }

    private void DrawNetwork(List<RouteEdge> edges)
    {
        EnsureCount(activeLines, edges.Count, "Active Supply Segment");
        EnsureCount(repeatLines, edges.Count, "Repeated Supply Segment");
        for (int i = 0; i < edges.Count; i++)
        {
            RouteEdge edge = edges[i];
            Vector3 a = Position(edge.First), b = Position(edge.Second);
            LineRenderer line = activeLines[i];
            line.enabled = true;
            Configure(line, activeColor, activeWidth, activeSortingOrder);
            line.positionCount = 2;
            line.SetPosition(0, a); line.SetPosition(1, b);
            LineRenderer repeat = repeatLines[i];
            repeat.enabled = edge.TraversalCount >= 2;
            if (!repeat.enabled) continue;
            Vector3 offset = Perpendicular(a, b);
            Configure(repeat, repeatColor, repeatWidth, repeatSortingOrder);
            repeat.positionCount = 2;
            repeat.SetPosition(0, a + offset); repeat.SetPosition(1, b + offset);
        }
        SetVisible(activeLines, edges.Count);
        for (int i = edges.Count; i < repeatLines.Count; i++) repeatLines[i].enabled = false;
    }

    private void EnsureCount(List<LineRenderer> lines, int count, string lineName) { while (lines.Count < count) lines.Add(CreateLine(lineName)); }
    private LineRenderer CreateLine(string lineName)
    {
        GameObject child = new(lineName);
        child.transform.SetParent(transform, false);
        LineRenderer line = child.AddComponent<LineRenderer>();
        line.sharedMaterial = GetMaterial(); line.useWorldSpace = true; line.alignment = LineAlignment.View; line.numCapVertices = 4; line.numCornerVertices = 4;
        return line;
    }
    private Material GetMaterial() { if (routeMaterial != null) return routeMaterial; if (runtimeMaterial == null && Shader.Find("Sprites/Default") is Shader shader) runtimeMaterial = new Material(shader); return runtimeMaterial; }
    private void Configure(LineRenderer line, Color color, float width, int order) { line.startColor = line.endColor = color; line.startWidth = line.endWidth = width; line.sortingLayerName = sortingLayerName; line.sortingOrder = order; }
    private static void SetVisible(List<LineRenderer> lines, int count) { for (int i = 0; i < lines.Count; i++) lines[i].enabled = i < count; }
    private Vector3 Position(ProvinceModel province)
    {
        CityCenter city = province != null ? province.GetComponentInChildren<CityCenter>(true) : null;
        Vector3 p = city != null ? city.transform.position : province != null ? province.GetProvincePosition() : Vector3.zero;
        p.z += routeZOffset;
        return p;
    }
    private Vector3 Perpendicular(Vector3 a, Vector3 b) { Vector2 d = new(b.x - a.x, b.y - a.y); if (d.sqrMagnitude < 0.0001f) return Vector3.zero; Vector2 p = new Vector2(-d.y, d.x).normalized * repeatOffset; return new Vector3(p.x, p.y, 0); }
}
