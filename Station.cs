using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// Put this on an empty object near the kitchen / counter / shop / trash.
// Player finds the nearest Station by distance (no colliders needed).
public class Station : MonoBehaviour
{
    public enum Type { Kitchen, Counter, Shop, Trash }
    public Type type;
    public float range = 2.0f;

    public static List<Station> All = new List<Station>();
    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public static Station Nearest(Vector3 p)
    {
        Station best = null; float bd = 1e9f;
        foreach (var s in All)
        {
            float d = Vector2.Distance(p, s.transform.position);
            if (d <= s.range && d < bd) { bd = d; best = s; }
        }
        return best;
    }

    public void Interact(Player pl, Keyboard kb, ref string hint)
    {
        var gm = GameManager.I;
        switch (type)
        {
            case Type.Kitchen:
                hint = "KITCHEN  1: Burger (" + gm.MeatCost(Dish.Burger) + " meat)   2: Steak (" + gm.MeatCost(Dish.Steak) + " meat)   3: Stew (" + gm.MeatCost(Dish.Stew) + " meat)";
                if (kb.digit1Key.wasPressedThisFrame) Cook(Dish.Burger);
                if (kb.digit2Key.wasPressedThisFrame) Cook(Dish.Steak);
                if (kb.digit3Key.wasPressedThisFrame) Cook(Dish.Stew);
                break;

            case Type.Counter:
                if (gm.counter.Count > 0 && !pl.holdingPlate)
                {
                    hint = "E: pick up " + gm.counter[0] + " (" + gm.counter.Count + " on counter)";
                    if (kb.eKey.wasPressedThisFrame)
                    {
                        pl.heldDish = gm.counter[0];
                        gm.counter.RemoveAt(0);
                        pl.holdingPlate = true;
                    }
                }
                else if (pl.holdingPlate) hint = "Hands full - serve a table or use the trash";
                else hint = "Counter is empty - cook something in the kitchen";
                break;

            case Type.Shop:
                hint = "SHOP  E: meat $" + gm.meatPrice + "   F: coffee $" + gm.coffeePrice + " (+0.5 sanity)   G: energy drink $" + gm.energyPrice + " (+1 sanity)";
                if (kb.eKey.wasPressedThisFrame)
                {
                    if (gm.money >= gm.meatPrice) { gm.money -= gm.meatPrice; gm.meat++; }
                    else gm.Say("Not enough cash");
                }
                if (kb.fKey.wasPressedThisFrame)
                {
                    if (gm.money >= gm.coffeePrice && gm.sanity < 3f) { gm.money -= gm.coffeePrice; gm.GainSanity(0.5f); }
                    else gm.Say(gm.sanity >= 3f ? "Sanity already full" : "Not enough cash");
                }
                if (kb.gKey.wasPressedThisFrame)
                {
                    if (gm.money >= gm.energyPrice && gm.sanity < 3f) { gm.money -= gm.energyPrice; gm.GainSanity(1f); }
                    else gm.Say(gm.sanity >= 3f ? "Sanity already full" : "Not enough cash");
                }
                break;

            case Type.Trash:
                if (pl.holdingPlate)
                {
                    hint = "E: throw away plate";
                    if (kb.eKey.wasPressedThisFrame) pl.holdingPlate = false;
                }
                break;
        }
    }

    void Cook(Dish d)
    {
        var gm = GameManager.I;
        if (gm.counter.Count >= 3) { gm.Say("Counter full (3 plates)"); return; }
        int cost = gm.MeatCost(d);
        if (gm.meat < cost) { gm.Say("Not enough meat!"); return; }
        gm.meat -= cost;
        gm.counter.Add(d);
    }

    // Small gizmo so you can see stations in the Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = type == Type.Kitchen ? Color.red : type == Type.Counter ? Color.yellow : type == Type.Shop ? Color.cyan : Color.gray;
        Gizmos.DrawWireSphere(transform.position, range);
    }

    static Sprite _white;
    public static Sprite WhiteSprite()
    {
        if (_white == null)
        {
            var t = new Texture2D(1, 1); t.SetPixel(0, 0, Color.white); t.Apply();
            _white = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 8f);
        }
        return _white;
    }
}
