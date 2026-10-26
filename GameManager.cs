using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public enum Dish { Burger, Steak, Stew }

// One per scene. Holds money, sanity, meat, timer and draws the whole HUD with OnGUI
// (no Canvas needed for the prototype).
public class GameManager : MonoBehaviour
{
    public static GameManager I;

    [Header("Level (Easy)")]
    public float shiftSeconds = 150f;
    public int cashGoal = 120;
    public int startMeat = 4;
    public int meatPrice = 6;
    public int coffeePrice = 25;
    public int energyPrice = 60;

    [Header("Dishes (meat cost / sale price)")]
    public int burgerMeat = 1, burgerPrice = 10;
    public int steakMeat = 2, steakPrice = 22;
    public int stewMeat = 1, stewPrice = 12;

    [HideInInspector] public int money;
    [HideInInspector] public int meat;
    [HideInInspector] public float sanity = 3f;
    [HideInInspector] public int platesOnCounter;       // max 3
    [HideInInspector] public List<Dish> counter = new List<Dish>();
    [HideInInspector] public float timeLeft;
    [HideInInspector] public string message = "";
    float messageUntil;
    public enum State { Playing, Won, Lost }
    [HideInInspector] public State state = State.Playing;
    string endReason = "";

    void Awake() { I = this; Time.timeScale = 1f; }

    void Start()
    {
        meat = startMeat;
        timeLeft = shiftSeconds;
    }

    public int MeatCost(Dish d) => d == Dish.Burger ? burgerMeat : d == Dish.Steak ? steakMeat : stewMeat;
    public int Price(Dish d) => d == Dish.Burger ? burgerPrice : d == Dish.Steak ? steakPrice : stewPrice;

    public void Say(string m) { message = m; messageUntil = Time.time + 2.5f; }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        if (state != State.Playing) return;
        timeLeft -= Time.deltaTime;
        if (money >= cashGoal) End(State.Won, "Cash goal reached!");
        else if (timeLeft <= 0f) End(State.Lost, "Shift over - goal not reached.");
    }

    public void AddMoney(int a) { money += a; if (money < 0) money = 0; }
    public void LoseSanity(float a)
    {
        sanity = Mathf.Max(0f, sanity - a);
        if (sanity <= 0f) End(State.Lost, "You lost your sanity.");
    }
    public void GainSanity(float a) { sanity = Mathf.Min(3f, sanity + a); }

    void End(State s, string why) { state = s; endReason = why; }

    // ---------------- HUD ----------------
    GUIStyle big, small, center;
    Texture2D white;
    void Init()
    {
        if (big != null) return;
        white = Texture2D.whiteTexture;
        big = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
        big.normal.textColor = Color.white;
        small = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
        small.normal.textColor = Color.white;
        center = new GUIStyle(big) { alignment = TextAnchor.MiddleCenter, fontSize = 40 };
    }
    void Fill(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, white); GUI.color = o; }

    void OnGUI()
    {
        Init();
        float W = Screen.width, H = Screen.height;

        // Top-left: stats
        Fill(new Rect(8, 8, 330, 118), new Color(0, 0, 0, 0.55f));
        GUI.Label(new Rect(16, 10, 320, 30), "Cash: $" + money + " / $" + cashGoal, big);
        int t = Mathf.Max(0, Mathf.CeilToInt(timeLeft));
        GUI.Label(new Rect(16, 38, 320, 30), "Time: " + (t / 60) + ":" + (t % 60).ToString("00"), big);
        GUI.Label(new Rect(16, 66, 320, 30), "Sanity: " + sanity.ToString("0.#") + " / 3", big);
        GUI.Label(new Rect(16, 94, 320, 30), "Meat: " + meat + "   Counter plates: " + counter.Count + "/3", small);

        // Top-center: order tickets
        float x = 360;
        foreach (var c in Customer.All)
        {
            if (c.state != Customer.State.Waiting) continue;
            var box = new Rect(x, 8, 120, 62);
            Fill(box, new Color(0, 0, 0, 0.6f));
            Fill(new Rect(box.x, box.y, 6, box.height), c.tableColor);
            GUI.Label(new Rect(box.x + 10, box.y, 110, 22), "Table " + c.tableNumber, small);
            GUI.Label(new Rect(box.x + 10, box.y + 20, 110, 22), c.order.ToString(), small);
            Fill(new Rect(box.x + 10, box.y + 46, 100, 8), Color.gray);
            var p = Mathf.Clamp01(c.patience);
            Fill(new Rect(box.x + 10, box.y + 46, 100 * p, 8), Color.Lerp(Color.red, Color.green, p));
            x += 126;
        }

        // Bottom: controls + interaction hint
        string hint = Player.I != null ? Player.I.hint : "";
        Fill(new Rect(0, H - 56, W, 56), new Color(0, 0, 0, 0.55f));
        GUI.Label(new Rect(12, H - 54, W - 24, 24), "WASD move | E interact | SPACE kill nearby customer | R restart", small);
        GUI.Label(new Rect(12, H - 30, W - 24, 24), hint, small);

        if (Time.time < messageUntil)
            GUI.Label(new Rect(0, H * 0.25f, W, 50), message, center);

        if (state != State.Playing)
        {
            Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.7f));
            string title = state == State.Won ? "SHIFT CLEARED!" : "GAME OVER";
            GUI.Label(new Rect(0, H * 0.3f, W, 60), title, center);
            int stars = state == State.Won ? (money >= cashGoal * 1.5f ? 3 : money >= cashGoal * 1.2f ? 2 : 1) : 0;
            GUI.Label(new Rect(0, H * 0.3f + 60, W, 40), endReason + "   Cash: $" + money + "   Stars: " + stars, new GUIStyle(center) { fontSize = 22 });
            GUI.Label(new Rect(0, H * 0.3f + 110, W, 40), "Press R to restart", new GUIStyle(center) { fontSize = 22 });
        }
    }
}
