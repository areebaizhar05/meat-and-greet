using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Walkable grid built from the floor tilemap. A cell is walkable when it has a floor tile,
/// the feet box around its centre stays on the floor, and no furniture collider overlaps it.
/// Customers use FindPath to walk around furniture instead of through it.
/// </summary>
public sealed class CafeNavGrid
{
    private readonly Tilemap floor;
    private readonly Vector2 clearance;
    private readonly HashSet<Vector3Int> walkable = new HashSet<Vector3Int>();

    private static readonly Vector3Int[] Neighbours =
    {
        new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
        new Vector3Int(1, 1, 0), new Vector3Int(-1, -1, 0), new Vector3Int(1, -1, 0), new Vector3Int(-1, 1, 0),
    };

    public int WalkableCount => walkable.Count;

    public CafeNavGrid(Tilemap floor, Vector2 clearance)
    {
        this.floor = floor;
        this.clearance = clearance;
        Rebuild();
    }

    /// <summary>Call again if furniture moves.</summary>
    public void Rebuild()
    {
        walkable.Clear();
        Physics2D.SyncTransforms();
        var bounds = floor.cellBounds;
        foreach (var cell in bounds.allPositionsWithin)
            if (floor.HasTile(cell) && IsClear(floor.GetCellCenterWorld(cell)))
                walkable.Add(cell);
    }

    private bool IsClear(Vector2 feet)
    {
        float hx = clearance.x * 0.5f, hy = clearance.y * 0.5f;
        if (!OnFloor(feet.x - hx, feet.y - hy) || !OnFloor(feet.x + hx, feet.y - hy) ||
            !OnFloor(feet.x - hx, feet.y + hy) || !OnFloor(feet.x + hx, feet.y + hy))
            return false;

        foreach (var hit in Physics2D.OverlapBoxAll(feet, clearance, 0f))
        {
            // Only furniture blocks: solid colliders with no Rigidbody2D (the player has one).
            if (hit != null && !hit.isTrigger && hit.attachedRigidbody == null)
                return false;
        }
        return true;
    }

    private bool OnFloor(float x, float y) => floor.HasTile(floor.WorldToCell(new Vector3(x, y, 0f)));

    public Vector3 CellCenter(Vector3Int cell)
    {
        var c = floor.GetCellCenterWorld(cell);
        c.z = 0f;
        return c;
    }

    /// <summary>The walkable cell closest to a world point.</summary>
    public bool NearestWalkable(Vector3 world, out Vector3Int best)
    {
        var start = floor.WorldToCell(world);
        best = start;
        float bestDist = float.PositiveInfinity;
        for (int r = 0; r <= 8; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                var c = start + new Vector3Int(dx, dy, 0);
                if (!walkable.Contains(c)) continue;
                float d = ((Vector2)(CellCenter(c) - world)).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = c; }
            }
            if (bestDist < float.PositiveInfinity) return true;
        }
        return false;
    }

    /// <summary>A* from one world point to another. Returns world-space waypoints, or null.</summary>
    public List<Vector3> FindPath(Vector3 from, Vector3 to)
    {
        if (!NearestWalkable(from, out var start) || !NearestWalkable(to, out var goal))
            return null;

        var open = new List<Vector3Int> { start };
        var came = new Dictionary<Vector3Int, Vector3Int>();
        var g = new Dictionary<Vector3Int, float> { [start] = 0f };
        var f = new Dictionary<Vector3Int, float> { [start] = Heuristic(start, goal) };
        var closed = new HashSet<Vector3Int>();

        while (open.Count > 0)
        {
            int bestIndex = 0;
            for (int i = 1; i < open.Count; i++)
                if (f[open[i]] < f[open[bestIndex]]) bestIndex = i;
            var current = open[bestIndex];
            open.RemoveAt(bestIndex);

            if (current == goal)
                return Build(came, current, to);

            closed.Add(current);
            foreach (var step in Neighbours)
            {
                var next = current + step;
                if (closed.Contains(next) || !walkable.Contains(next)) continue;

                // No cutting corners past furniture on diagonal steps.
                if (step.x != 0 && step.y != 0 &&
                    (!walkable.Contains(current + new Vector3Int(step.x, 0, 0)) ||
                     !walkable.Contains(current + new Vector3Int(0, step.y, 0))))
                    continue;

                float cost = g[current] + Vector2.Distance(CellCenter(current), CellCenter(next));
                if (g.TryGetValue(next, out var old) && cost >= old) continue;

                came[next] = current;
                g[next] = cost;
                f[next] = cost + Heuristic(next, goal);
                if (!open.Contains(next)) open.Add(next);
            }
        }
        return null;
    }

    private float Heuristic(Vector3Int a, Vector3Int b) => Vector2.Distance(CellCenter(a), CellCenter(b));

    private List<Vector3> Build(Dictionary<Vector3Int, Vector3Int> came, Vector3Int end, Vector3 exactEnd)
    {
        var cells = new List<Vector3Int> { end };
        while (came.TryGetValue(end, out var prev)) { end = prev; cells.Add(end); }
        cells.Reverse();

        var points = new List<Vector3>(cells.Count + 1);
        for (int i = 1; i < cells.Count; i++)   // skip the start cell, we are already there
            points.Add(CellCenter(cells[i]));
        points.Add(exactEnd);
        return points;
    }
}
