using UnityEngine;
using System.Collections.Generic;

public class Customer : MonoBehaviour
{
    public enum State { Entering, Waiting, Eating, Leaving }

    public static List<Customer> All = new List<Customer>();
    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    [Header("Set on the prefab")]
    public bool isAnomaly;
    public float walkSpeed = 3f;

    [Header("Tuning")]
    public float patienceSeconds = 40f;   // time until they storm out
    public float eatSeconds = 4f;

    // runtime
    [HideInInspector] public State state = State.Entering;
    [HideInInspector] public Dish order;
    [HideInInspector] public float patience = 1f;   // 1 = happy, 0 = gone
    [HideInInspector] public int tableNumber;
    [HideInInspector] public Color tableColor = Color.white;

    Vector3 seatPos, exitPos;
    CustomerSpawner spawner;
    int seatIndex;
    float eatTimer;
    bool paid;

    public void Init(CustomerSpawner sp, int seatIdx, Vector3 seat, Vector3 exit, Color col)
    {
        spawner = sp; seatIndex = seatIdx; seatPos = seat; exitPos = exit;
        tableNumber = seatIdx + 1; tableColor = col;
        order = (Dish)Random.Range(0, 3);

        // coloured marker above head = table colour (matches the order ticket)
        var m = new GameObject("TableMarker");
        m.transform.SetParent(transform, false);
        var sr = m.AddComponent<SpriteRenderer>();
        sr.sprite = Station.WhiteSprite();
        sr.color = col;
        sr.sortingOrder = 40;
        var b = GetComponentInChildren<SpriteRenderer>().bounds;
        m.transform.position = new Vector3(transform.position.x, b.max.y + 0.25f, 0);
        m.transform.localScale = Vector3.one * 2.5f / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm != null && gm.state != GameManager.State.Playing) return;

        switch (state)
        {
            case State.Entering:
                if (MoveTo(seatPos)) state = State.Waiting;
                break;

            case State.Waiting:
                patience -= Time.deltaTime / patienceSeconds;
                if (patience <= 0f)
                {
                    GameManager.I.Say("Table " + tableNumber + " stormed out without paying!");
                    Leave();
                }
                break;

            case State.Eating:
                eatTimer -= Time.deltaTime;
                if (eatTimer <= 0f) Leave();
                break;

            case State.Leaving:
                if (MoveTo(exitPos)) Destroy(gameObject);
                break;
        }
    }

    bool MoveTo(Vector3 target)
    {
        target.z = transform.position.z;
        var dir = target - transform.position;
        if (Mathf.Abs(dir.x) > 0.02f)
        {
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (dir.x < 0 ? -1 : 1);
            transform.localScale = s;
        }
        transform.position = Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);
        return Vector3.Distance(transform.position, target) < 0.03f;
    }

    void Leave()
    {
        state = State.Leaving;
        if (spawner != null) spawner.FreeSeat(seatIndex);
        var mk = transform.Find("TableMarker");
        if (mk != null) Destroy(mk.gameObject);
    }

    // ---------- called by Player ----------
    public void Serve(Dish d)
    {
        var gm = GameManager.I;
        if (state != State.Waiting) return;

        if (isAnomaly)
        {
            gm.Say("Served an anomaly... that was wasted food.");
            StartEating(false);
            return;
        }
        if (d != order)
        {
            patience = Mathf.Max(0.05f, patience - 0.25f);   // mood drops a level
            gm.Say("Wrong dish! Table " + tableNumber + " wanted " + order);
            return;                                           // plate is lost
        }
        StartEating(true);
    }

    void StartEating(bool pay)
    {
        var gm = GameManager.I;
        if (pay && !paid)
        {
            paid = true;
            // payment depends on mood: 50% .. 100% (+ small tip when very happy)
            float mult = 0.5f + 0.5f * Mathf.Clamp01(patience);
            int amt = Mathf.RoundToInt(gm.Price(order) * mult) + (patience > 0.8f ? 2 : 0);
            gm.AddMoney(amt);
            gm.Say("+$" + amt);
        }
        state = State.Eating;
        eatTimer = eatSeconds;
    }

    public void Kill()
    {
        var gm = GameManager.I;
        if (gm.state != GameManager.State.Playing) return;

        if (isAnomaly)
        {
            gm.meat += 1;
            gm.Say("Anomaly eliminated! +1 meat");
            MeatPop.Spawn(transform.position, new Color(0.85f, 0.15f, 0.2f));
        }
        else
        {
            gm.Say("That was a normal customer... -1 sanity");
            MeatPop.Spawn(transform.position, new Color(0.6f, 0.6f, 0.6f));
            gm.LoseSanity(1f);
        }
        if (spawner != null && state != State.Leaving) spawner.FreeSeat(seatIndex);
        Destroy(gameObject);
    }

    public static Customer Nearest(Vector3 p, float range)
    {
        Customer best = null; float bd = 1e9f;
        foreach (var c in All)
        {
            float d = Vector2.Distance(p, c.transform.position);
            if (d <= range && d < bd) { bd = d; best = c; }
        }
        return best;
    }
}

// Cartoon "pop": a few little chunks fly out and fade.
public class MeatPop : MonoBehaviour
{
    struct Bit { public Transform t; public Vector2 v; public SpriteRenderer sr; }
    Bit[] bits; float life = 0.8f, age;

    public static void Spawn(Vector3 pos, Color col)
    {
        var go = new GameObject("MeatPop");
        go.transform.position = pos;
        var mp = go.AddComponent<MeatPop>();
        mp.bits = new Bit[8];
        for (int i = 0; i < mp.bits.Length; i++)
        {
            var b = new GameObject("bit");
            b.transform.SetParent(go.transform, false);
            var sr = b.AddComponent<SpriteRenderer>();
            sr.sprite = Station.WhiteSprite();
            sr.color = col; sr.sortingOrder = 60;
            b.transform.localScale = Vector3.one * Random.Range(2f, 4f);
            mp.bits[i] = new Bit { t = b.transform, sr = sr, v = Random.insideUnitCircle.normalized * Random.Range(1.5f, 3.5f) + Vector2.up * 2f };
        }
    }

    void Update()
    {
        age += Time.deltaTime;
        for (int i = 0; i < bits.Length; i++)
        {
            bits[i].v.y -= 6f * Time.deltaTime;
            bits[i].t.position += (Vector3)(bits[i].v * Time.deltaTime);
            var col = bits[i].sr.color; col.a = 1f - age / life; bits[i].sr.color = col;
        }
        if (age >= life) Destroy(gameObject);
    }
}
