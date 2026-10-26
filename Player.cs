using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    public static Player I;
    public float speed = 5f;
    public float interactRange = 1.6f;
    public float killRange = 1.3f;

    [HideInInspector] public string hint = "";
    [HideInInspector] public bool holdingPlate;
    [HideInInspector] public Dish heldDish;

    Rigidbody2D rb;
    Vector2 move;
    GameObject plateIcon;

    void Awake()
    {
        I = this;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        var gm = GameManager.I;
        if (gm == null || gm.state != GameManager.State.Playing) { move = Vector2.zero; return; }

        move = Vector2.zero;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
        move = move.normalized;

        // flip to face movement
        if (Mathf.Abs(move.x) > 0.01f)
        {
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (move.x < 0 ? -1 : 1);
            transform.localScale = s;
        }

        Customer nearCust = Customer.Nearest(transform.position, interactRange);
        Station nearStation = Station.Nearest(transform.position);
        hint = "";

        // ---- kill ----
        Customer killTarget = Customer.Nearest(transform.position, killRange);
        if (killTarget != null)
        {
            hint = "SPACE: kill customer";
            if (kb.spaceKey.wasPressedThisFrame) killTarget.Kill();
        }

        // ---- serve a seated customer ----
        if (nearCust != null && nearCust.state == Customer.State.Waiting)
        {
            if (holdingPlate)
            {
                hint = "E: serve " + heldDish + " to table " + nearCust.tableNumber + (killTarget != null ? "   |   SPACE: kill" : "");
                if (kb.eKey.wasPressedThisFrame)
                {
                    nearCust.Serve(heldDish);
                    holdingPlate = false;
                }
            }
            else if (killTarget == null) hint = "Get a plate from the kitchen counter first";
        }
        else if (nearCust != null && nearCust.state == Customer.State.Eating)
        {
            // nothing to do while eating
        }

        // ---- stations ----
        if (nearStation != null) nearStation.Interact(this, kb, ref hint);

        UpdateIcon();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = move * speed;
    }

    // Tiny coloured square above the head shows you're carrying a plate.
    void UpdateIcon()
    {
        if (holdingPlate && plateIcon == null)
        {
            plateIcon = new GameObject("PlateIcon");
            plateIcon.transform.SetParent(transform, false);
            var sr = plateIcon.AddComponent<SpriteRenderer>();
            sr.sprite = Station.WhiteSprite();
            sr.sortingOrder = 50;
            float top = transform.position.y;
            foreach (var r in GetComponentsInChildren<SpriteRenderer>()) top = Mathf.Max(top, r.bounds.max.y);
            plateIcon.transform.position = new Vector3(transform.position.x, top + 0.3f, 0);
            plateIcon.transform.localScale = Vector3.one * 3f / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        }
        if (plateIcon != null)
        {
            plateIcon.SetActive(holdingPlate);
            if (holdingPlate)
                plateIcon.GetComponent<SpriteRenderer>().color =
                    heldDish == Dish.Burger ? new Color(1f, .75f, .2f) : heldDish == Dish.Steak ? new Color(.8f, .2f, .2f) : new Color(.6f, .4f, .2f);
        }
    }
}
