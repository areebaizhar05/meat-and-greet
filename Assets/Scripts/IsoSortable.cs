using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on anything that stands on the floor (counters, plants, fridges) or on top of
/// something else (a cup on a counter). The IsoDepthSorter uses it to set sorting orders
/// every frame so the player draws in front of or behind each prop correctly.
///
/// Floor props: give them a PolygonCollider2D that covers only the base of the object.
/// Things sitting on a prop: leave the collider off and set "Attach To" to the prop below.
/// </summary>
[DisallowMultipleComponent]
public sealed class IsoSortable : MonoBehaviour
{
    [Tooltip("Set this for things that sit on, or hang from, another prop. They always draw just above it.")]
    [SerializeField] private IsoSortable attachTo;

    [Tooltip("How far above the host to draw. Use 1, 2, 3... to stack several items on one host.")]
    [SerializeField, Range(0, 9)] private int attachOffset = 1;

    [Tooltip("Off for anything that moves (player, customers). Static props are measured once.")]
    [SerializeField] private bool isStatic = true;

    internal IsoSortable AttachTo => attachTo;
    internal int AttachOffset => attachOffset;
    internal bool IsStatic => isStatic;

    internal SpriteRenderer[] Renderers;
    internal Collider2D[] Footprints;
    internal readonly List<IsoSortable> Attached = new List<IsoSortable>();

    // Cached measurements, filled by the sorter.
    internal bool Measured;
    internal Vector2 GridMin, GridMax;   // footprint box in isometric grid space
    internal Bounds ScreenBounds;        // union of sprite bounds, world space
    internal int Rank;

    /// <summary>Call on objects added at runtime that move, such as the player.</summary>
    public void MarkMoving()
    {
        isStatic = false;
        Measured = false;
    }

    private void Awake() => CacheComponents();

    internal void CacheComponents()
    {
        Renderers = GetComponentsInChildren<SpriteRenderer>(true);
        var all = GetComponents<Collider2D>();
        var solid = new List<Collider2D>();
        foreach (var c in all)
            if (!c.isTrigger)
                solid.Add(c);
        Footprints = solid.ToArray();
        Measured = false;
    }

    private void OnEnable() => IsoDepthSorter.Register(this);
    private void OnDisable() => IsoDepthSorter.Unregister(this);

    private void OnValidate()
    {
        if (attachTo == this)
            attachTo = null;
    }
}
