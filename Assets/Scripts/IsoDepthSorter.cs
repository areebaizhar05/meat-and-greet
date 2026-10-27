using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sorts every IsoSortable in the scene each frame. Created automatically, you never add it by hand.
///
/// How it works: each floor object has a footprint (its collider). The footprint is turned into a
/// box in isometric grid space. If box A lies fully past box B along either grid axis, A is behind B.
/// Those "behind" pairs are ordered with a topological sort, then each object gets a sorting order.
/// </summary>
[DefaultExecutionOrder(1000)]
public sealed class IsoDepthSorter : MonoBehaviour
{
    public const int FirstOrder = 0;
    public const int OrderStep = 10;

    private static readonly List<IsoSortable> All = new List<IsoSortable>();
    private static IsoDepthSorter instance;

    private readonly List<IsoSortable> roots = new List<IsoSortable>();
    private List<int>[] edges = new List<int>[0];
    private int[] incoming = new int[0];
    private Vector2 cell = new Vector2(1f, 0.5f);

    internal static void Register(IsoSortable s)
    {
        if (!All.Contains(s))
            All.Add(s);
        if (instance == null && Application.isPlaying)
        {
            var go = new GameObject("[IsoDepthSorter]");
            instance = go.AddComponent<IsoDepthSorter>();
        }
    }

    internal static void Unregister(IsoSortable s) => All.Remove(s);

    private void Awake()
    {
        var grid = FindFirstObjectByType<Grid>();
        if (grid != null)
            cell = new Vector2(Mathf.Abs(grid.cellSize.x), Mathf.Abs(grid.cellSize.y));
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void LateUpdate()
    {
        // 1. Split into roots (things on the floor) and attached items.
        roots.Clear();
        foreach (var s in All)
            s.Attached.Clear();
        foreach (var s in All)
        {
            var host = FindRoot(s);
            if (host == s)
                roots.Add(s);
            else
                host.Attached.Add(s);
        }

        // 2. Measure footprints and screen bounds.
        foreach (var r in roots)
            if (!r.IsStatic || !r.Measured || HasMovingAttachment(r))
                Measure(r);

        // 3. Build "drawn before" edges between overlapping pairs.
        int n = roots.Count;
        if (edges.Length < n)
        {
            edges = new List<int>[n];
            incoming = new int[n];
        }
        for (int i = 0; i < n; i++)
        {
            if (edges[i] == null) edges[i] = new List<int>();
            edges[i].Clear();
            incoming[i] = 0;
        }

        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                var a = roots[i];
                var b = roots[j];
                if (!Overlaps2D(a.ScreenBounds, b.ScreenBounds))
                    continue;

                int cmp = Compare(a, b); // -1: a behind b, 1: b behind a
                if (cmp < 0) { edges[i].Add(j); incoming[j]++; }
                else if (cmp > 0) { edges[j].Add(i); incoming[i]++; }
            }
        }

        // 4. Topological sort. Among ready items, draw the one furthest back first.
        var done = new bool[n];
        for (int rank = 0; rank < n; rank++)
        {
            int pick = -1;
            float best = float.NegativeInfinity;
            for (int i = 0; i < n; i++)
            {
                if (done[i] || incoming[i] > 0) continue;
                float depth = Depth(roots[i]);
                if (depth > best) { best = depth; pick = i; }
            }

            // A cycle (overlapping footprints). Break it with the furthest-back item.
            if (pick < 0)
            {
                for (int i = 0; i < n; i++)
                {
                    if (done[i]) continue;
                    float depth = Depth(roots[i]);
                    if (depth > best) { best = depth; pick = i; }
                }
            }

            done[pick] = true;
            roots[pick].Rank = rank;
            foreach (var k in edges[pick])
                incoming[k]--;
        }

        // 5. Apply sorting orders.
        foreach (var r in roots)
        {
            int order = FirstOrder + r.Rank * OrderStep;
            SetOrder(r, order);
            foreach (var child in r.Attached)
                SetOrder(child, order + Mathf.Clamp(child.AttachOffset, 0, OrderStep - 1));
        }
    }

    private static IsoSortable FindRoot(IsoSortable s)
    {
        var cur = s;
        for (int guard = 0; guard < 16 && cur.AttachTo != null && cur.AttachTo.isActiveAndEnabled; guard++)
            cur = cur.AttachTo;
        return cur;
    }

    private static bool HasMovingAttachment(IsoSortable r)
    {
        foreach (var c in r.Attached)
            if (!c.IsStatic) return true;
        return false;
    }

    private static void SetOrder(IsoSortable s, int order)
    {
        if (s.Renderers == null) s.CacheComponents();
        foreach (var sr in s.Renderers)
            if (sr != null) sr.sortingOrder = order;
    }

    private void Measure(IsoSortable r)
    {
        if (r.Renderers == null) r.CacheComponents();

        bool hasBox = false;
        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        AddFootprint(r, ref min, ref max, ref hasBox);
        foreach (var c in r.Attached)
            AddFootprint(c, ref min, ref max, ref hasBox);

        bool hasScreen = false;
        var screen = new Bounds();
        AddScreen(r, ref screen, ref hasScreen);
        foreach (var c in r.Attached)
            AddScreen(c, ref screen, ref hasScreen);

        // No collider: use the bottom-centre of the sprite as a single point.
        if (!hasBox)
        {
            var p = hasScreen ? new Vector2(screen.center.x, screen.min.y) : (Vector2)r.transform.position;
            min = max = ToGrid(p);
        }

        r.GridMin = min;
        r.GridMax = max;
        r.ScreenBounds = hasScreen ? screen : new Bounds(r.transform.position, Vector3.zero);
        r.Measured = true;
    }

    private void AddFootprint(IsoSortable s, ref Vector2 min, ref Vector2 max, ref bool any)
    {
        if (s.Footprints == null) s.CacheComponents();
        foreach (var col in s.Footprints)
        {
            if (col == null || !col.enabled) continue;
            if (col is PolygonCollider2D poly && poly.pathCount > 0)
            {
                for (int p = 0; p < poly.pathCount; p++)
                    foreach (var local in poly.GetPath(p))
                        Include(ToGrid(poly.transform.TransformPoint(local + poly.offset)), ref min, ref max);
            }
            else
            {
                var b = col.bounds;
                Include(ToGrid(new Vector2(b.min.x, b.min.y)), ref min, ref max);
                Include(ToGrid(new Vector2(b.max.x, b.min.y)), ref min, ref max);
                Include(ToGrid(new Vector2(b.min.x, b.max.y)), ref min, ref max);
                Include(ToGrid(new Vector2(b.max.x, b.max.y)), ref min, ref max);
            }
            any = true;
        }
    }

    private static void AddScreen(IsoSortable s, ref Bounds screen, ref bool any)
    {
        foreach (var sr in s.Renderers)
        {
            if (sr == null || !sr.enabled || sr.sprite == null) continue;
            if (!any) { screen = sr.bounds; any = true; }
            else screen.Encapsulate(sr.bounds);
        }
    }

    private static void Include(Vector2 g, ref Vector2 min, ref Vector2 max)
    {
        min = Vector2.Min(min, g);
        max = Vector2.Max(max, g);
    }

    /// <summary>World position to isometric grid axes. Larger values are further back.</summary>
    private Vector2 ToGrid(Vector2 world)
    {
        float u = world.x / cell.x;
        float v = world.y / cell.y;
        return new Vector2(v + u, v - u);
    }

    private static float Depth(IsoSortable s) => (s.GridMin.x + s.GridMax.x + s.GridMin.y + s.GridMax.y) * 0.25f;

    private static bool Overlaps2D(Bounds a, Bounds b)
    {
        return a.min.x < b.max.x && b.min.x < a.max.x && a.min.y < b.max.y && b.min.y < a.max.y;
    }

    /// <summary>-1 if a is behind b, 1 if b is behind a, 0 if it does not matter.</summary>
    private static int Compare(IsoSortable a, IsoSortable b)
    {
        const float eps = 0.01f;
        bool aBehind = a.GridMin.x >= b.GridMax.x - eps || a.GridMin.y >= b.GridMax.y - eps;
        bool bBehind = b.GridMin.x >= a.GridMax.x - eps || b.GridMin.y >= a.GridMax.y - eps;
        if (aBehind && !bBehind) return -1;
        if (bBehind && !aBehind) return 1;

        // Boxes overlap or sit side by side. Test the smaller one's centre against the bigger box.
        bool aSmaller = Area(a) <= Area(b);
        var small = aSmaller ? a : b;
        var big = aSmaller ? b : a;
        var c = (small.GridMin + small.GridMax) * 0.5f;
        int smallVsBig;
        if (c.x >= big.GridMax.x || c.y >= big.GridMax.y) smallVsBig = -1;      // small behind big
        else if (c.x <= big.GridMin.x || c.y <= big.GridMin.y) smallVsBig = 1;  // small in front
        else smallVsBig = Depth(small) >= Depth(big) ? -1 : 1;
        return aSmaller ? smallVsBig : -smallVsBig;
    }

    private static float Area(IsoSortable s) => (s.GridMax.x - s.GridMin.x) * (s.GridMax.y - s.GridMin.y);
}
