#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Tools > Meat and Greet > Restore Furniture Colliders.
/// Adds the footprint colliders and IsoSortable components to the furniture in the open scene,
/// matched by object name. Safe to run more than once. Supports Undo (Ctrl+Z).
/// </summary>
public static class FurnitureColliderSetup
{
    // Footprint shapes in each object's local space (only the base of the furniture).
    private static readonly Dictionary<string, Vector2[]> Footprints = new Dictionary<string, Vector2[]>
    {
            { "bar_counter_single_seat_0", new[] { new Vector2(-0.4900f, -0.3400f), new Vector2(-0.1800f, -0.5300f), new Vector2(0.0900f, -0.6500f), new Vector2(0.3200f, -0.7600f), new Vector2(0.5600f, -0.6400f), new Vector2(0.5300f, -0.2000f), new Vector2(0.2100f, -0.0100f) } },
            { "bar_counter_single_seat_0 (1)", new[] { new Vector2(-0.4900f, -0.3400f), new Vector2(-0.1800f, -0.5300f), new Vector2(0.0900f, -0.6500f), new Vector2(0.3200f, -0.7600f), new Vector2(0.5600f, -0.6400f), new Vector2(0.5300f, -0.2000f), new Vector2(0.2100f, -0.0100f) } },
            { "bar_counter_single_seat_0 (2)", new[] { new Vector2(-0.4900f, -0.3400f), new Vector2(-0.1800f, -0.5300f), new Vector2(0.0900f, -0.6500f), new Vector2(0.3200f, -0.7600f), new Vector2(0.5600f, -0.6400f), new Vector2(0.5300f, -0.2000f), new Vector2(0.2100f, -0.0100f) } },
            { "bar_counter_single_seat_0 (3)", new[] { new Vector2(-0.4900f, -0.3400f), new Vector2(-0.1800f, -0.5300f), new Vector2(0.0900f, -0.6500f), new Vector2(0.3200f, -0.7600f), new Vector2(0.5600f, -0.6400f), new Vector2(0.5300f, -0.2000f), new Vector2(0.2100f, -0.0100f) } },
            { "bar_counter_single_seat_0 (4)", new[] { new Vector2(-0.4900f, -0.3400f), new Vector2(-0.1800f, -0.5300f), new Vector2(0.0900f, -0.6500f), new Vector2(0.3200f, -0.7600f), new Vector2(0.5600f, -0.6400f), new Vector2(0.5300f, -0.2000f), new Vector2(0.2100f, -0.0100f) } },
            { "bar_counter_single_seat_0 (5)", new[] { new Vector2(-0.4900f, -0.3400f), new Vector2(-0.1800f, -0.5300f), new Vector2(0.0900f, -0.6500f), new Vector2(0.3200f, -0.7600f), new Vector2(0.5600f, -0.6400f), new Vector2(0.5300f, -0.2000f), new Vector2(0.2100f, -0.0100f) } },
            { "prop_14_0", new[] { new Vector2(-0.4700f, -0.1800f), new Vector2(0.1300f, -0.4300f), new Vector2(0.4900f, -0.1100f), new Vector2(-0.1100f, 0.1400f) } },
            { "prop_14_0 (1)", new[] { new Vector2(-0.4700f, -0.1800f), new Vector2(0.1300f, -0.4300f), new Vector2(0.4900f, -0.1100f), new Vector2(-0.1100f, 0.1400f) } },
            { "prop_14_0 (2)", new[] { new Vector2(-0.4700f, -0.1800f), new Vector2(0.1300f, -0.4300f), new Vector2(0.4900f, -0.1100f), new Vector2(-0.1100f, 0.1400f) } },
            { "prop_14_0 (3)", new[] { new Vector2(-0.4700f, -0.1800f), new Vector2(0.1300f, -0.4300f), new Vector2(0.4900f, -0.1100f), new Vector2(-0.1100f, 0.1400f) } },
            { "prop_14_0 (4)", new[] { new Vector2(-0.4700f, -0.1800f), new Vector2(0.1300f, -0.4300f), new Vector2(0.4900f, -0.1100f), new Vector2(-0.1100f, 0.1400f) } },
            { "prop_04_0", new[] { new Vector2(-0.2300f, -0.2650f), new Vector2(0.0100f, -0.4150f), new Vector2(0.3400f, -0.1050f), new Vector2(0.1000f, 0.0450f) } },
            { "prop_04_0 (2)", new[] { new Vector2(-0.2300f, -0.2650f), new Vector2(0.0100f, -0.4150f), new Vector2(0.3400f, -0.1050f), new Vector2(0.1000f, 0.0450f) } },
            { "prop_04_0 (3)", new[] { new Vector2(-0.2300f, -0.2650f), new Vector2(0.0100f, -0.4150f), new Vector2(0.3400f, -0.1050f), new Vector2(0.1000f, 0.0450f) } },
            { "prop_04_0 (4)", new[] { new Vector2(-0.2300f, -0.2650f), new Vector2(0.0100f, -0.4150f), new Vector2(0.3400f, -0.1050f), new Vector2(0.1000f, 0.0450f) } },
            { "cabinet", new[] { new Vector2(-2.4250f, -3.3350f), new Vector2(-0.6450f, -4.4150f), new Vector2(2.4250f, -2.6350f), new Vector2(0.6450f, -1.5550f) } },
            { "cabinet (1)", new[] { new Vector2(-2.4250f, -3.3350f), new Vector2(-0.6450f, -4.4150f), new Vector2(2.4250f, -2.6350f), new Vector2(0.6450f, -1.5550f) } },
            { "counter_medium_plain_0", new[] { new Vector2(-1.8200f, -1.0450f), new Vector2(-1.2300f, -1.4050f), new Vector2(1.7900f, 0.1650f), new Vector2(1.2000f, 0.5250f) } },
            { "kitchen_cabinet_medium_0", new[] { new Vector2(-1.6100f, -0.8400f), new Vector2(-1.1300f, -1.1500f), new Vector2(1.5800f, 0.0000f), new Vector2(1.1000f, 0.3100f) } },
            { "wall2", new[] { new Vector2(-1.5850f, -6.8800f), new Vector2(1.5950f, -5.0500f), new Vector2(1.3350f, -4.9200f), new Vector2(-1.8450f, -6.7500f) } },
            { "kitchen_cabinet_medium_0 (1)", new[] { new Vector2(-1.6100f, -0.8400f), new Vector2(-1.1300f, -1.1500f), new Vector2(1.5800f, 0.0000f), new Vector2(1.1000f, 0.3100f) } },
            { "wall2 (1)", new[] { new Vector2(-1.5850f, -6.8800f), new Vector2(1.5950f, -5.0500f), new Vector2(1.3350f, -4.9200f), new Vector2(-1.8450f, -6.7500f) } },
            { "wall2 (2)", new[] { new Vector2(-1.5850f, -6.8800f), new Vector2(1.5950f, -5.0500f), new Vector2(1.3350f, -4.9200f), new Vector2(-1.8450f, -6.7500f) } },
    };

    // Furniture that stands on the floor and gets sorted on its own.
    private static readonly string[] Hosts =
    {
            "bar_counter_single_seat_0",
            "bar_counter_single_seat_0 (1)",
            "bar_counter_single_seat_0 (2)",
            "bar_counter_single_seat_0 (3)",
            "bar_counter_single_seat_0 (4)",
            "bar_counter_single_seat_0 (5)",
            "prop_14_0",
            "prop_14_0 (1)",
            "prop_14_0 (2)",
            "prop_14_0 (3)",
            "prop_14_0 (4)",
            "prop_04_0",
            "prop_04_0 (2)",
            "prop_04_0 (3)",
            "prop_04_0 (4)",
            "cabinet",
            "cabinet (1)",
            "counter_medium_plain_0",
            "kitchen_cabinet_medium_0",
            "wall2",
    };

    // Things that sit on, or hang from, another piece: (host name, draw offset above it).
    private static readonly Dictionary<string, (string host, int offset)> Attached = new Dictionary<string, (string host, int offset)>
    {
            { "kitchen_cabinet_medium_0 (1)", ("kitchen_cabinet_medium_0", 1) },
            { "sink", ("kitchen_cabinet_medium_0", 2) },
            { "prop_08_0", ("kitchen_cabinet_medium_0", 2) },
            { "prop_07_0", ("kitchen_cabinet_medium_0", 2) },
            { "Isometric_Cafe_Counter_Tileset_10", ("kitchen_cabinet_medium_0", 2) },
            { "Isometric_Cafe_Counter_Tileset_10 (1)", ("kitchen_cabinet_medium_0", 3) },
            { "hang1", ("kitchen_cabinet_medium_0", 2) },
            { "hang2", ("kitchen_cabinet_medium_0", 2) },
            { "wall2 (1)", ("wall2", 1) },
            { "wall2 (2)", ("wall2", 2) },
            { "menu2", ("wall2", 3) },
            { "prop_05_0", ("counter_medium_plain_0", 1) },
            { "Isometric_Cafe_Counter_Tileset_6", ("counter_medium_plain_0", 1) },
            { "prop_11_0", ("counter_medium_plain_0", 2) },
            { "prop_04_0 (1)", ("counter_medium_plain_0", 3) },
    };

    private static readonly string[] WallDecor = { "window", "window (1)", "window (2)", "menu1" };

    [MenuItem("Tools/Meat and Greet/Restore Furniture Colliders")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var byName = new Dictionary<string, GameObject>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (!byName.ContainsKey(t.name)) byName[t.name] = t.gameObject;

        int colliders = 0, sortables = 0;
        var missing = new List<string>();
        var sortableOf = new Dictionary<string, IsoSortable>();

        // 1. Footprint colliders
        foreach (var kv in Footprints)
        {
            if (!byName.TryGetValue(kv.Key, out var go)) { missing.Add(kv.Key); continue; }
            var poly = go.GetComponent<PolygonCollider2D>();
            if (poly == null) poly = Undo.AddComponent<PolygonCollider2D>(go);
            else Undo.RecordObject(poly, "Restore footprint");
            poly.isTrigger = false;
            poly.offset = Vector2.zero;
            poly.pathCount = 1;
            poly.SetPath(0, kv.Value);
            colliders++;
        }

        // 2. IsoSortable on furniture and on the things sitting on it
        var all = new List<string>(Hosts);
        all.AddRange(Attached.Keys);
        foreach (var name in all)
        {
            if (!byName.TryGetValue(name, out var go)) { if (!missing.Contains(name)) missing.Add(name); continue; }
            var s = go.GetComponent<IsoSortable>();
            if (s == null) s = Undo.AddComponent<IsoSortable>(go);
            sortableOf[name] = s;
            sortables++;
        }

        // 3. IsoSortable settings (private fields, so set through SerializedObject)
        foreach (var kv in sortableOf)
        {
            var so = new SerializedObject(kv.Value);
            so.FindProperty("isStatic").boolValue = true;
            if (Attached.TryGetValue(kv.Key, out var a) && sortableOf.TryGetValue(a.host, out var host))
            {
                so.FindProperty("attachTo").objectReferenceValue = host;
                so.FindProperty("attachOffset").intValue = a.offset;
            }
            else
            {
                so.FindProperty("attachTo").objectReferenceValue = null;
            }
            so.ApplyModifiedProperties();
        }

        // 4. Fixed draw orders: floor, walls, wall decorations
        foreach (var kv in byName)
        {
            var tr = kv.Value.GetComponent<TilemapRenderer>();
            if (tr == null) continue;
            Undo.RecordObject(tr, "Tilemap order");
            tr.sortingOrder = kv.Key == "floor" ? -100 : -90;
        }
        foreach (var name in WallDecor)
        {
            if (byName.TryGetValue(name, out var go) && go.TryGetComponent<SpriteRenderer>(out var sr))
            {
                Undo.RecordObject(sr, "Wall decor order");
                sr.sortingOrder = -80;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        string msg = "Footprint colliders: " + colliders + "\nIsoSortables: " + sortables;
        if (missing.Count > 0) msg += "\n\nNot found in this scene:\n" + string.Join("\n", missing);
        msg += "\n\nSave the scene (Ctrl+S) to keep the changes.";
        Debug.Log("[FurnitureColliderSetup] " + msg.Replace("\n", " | "));
        EditorUtility.DisplayDialog("Restore Furniture Colliders", msg, "OK");
    }
}
#endif
