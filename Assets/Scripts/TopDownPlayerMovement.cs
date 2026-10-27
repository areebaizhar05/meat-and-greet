using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Moves in screen-cardinal directions while keeping the player on the 2D grid.</summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class TopDownPlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;

    private Rigidbody2D body;
    private Vector2 moveInput;

    public Vector2 MoveInput => moveInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        var playerCollider = GetComponent<BoxCollider2D>();
        if (playerCollider == null)
            playerCollider = gameObject.AddComponent<BoxCollider2D>();
        FitColliderToFeet(playerCollider, GetCollisionSprites());
    }

    private void FitColliderToFeet(BoxCollider2D playerCollider, SpriteRenderer[] renderers)
    {
        if (renderers.Length == 0)
            return;

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        // The full character art is tall, but collisions should happen at foot level.
        var height = Mathf.Min(0.9f, bounds.size.y * 0.3f);
        var width = bounds.size.x * 0.65f;
        var footCenter = new Vector3(bounds.center.x, bounds.min.y + height * 0.5f, transform.position.z);
        var localCenter = transform.InverseTransformPoint(footCenter);
        var localSize = transform.InverseTransformVector(new Vector3(width, height, 0f));
        playerCollider.offset = new Vector2(localCenter.x, localCenter.y);
        playerCollider.size = new Vector2(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y));
    }

    private SpriteRenderer[] GetCollisionSprites()
    {
        var allSprites = GetComponentsInChildren<SpriteRenderer>();
        var coreSprites = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (var sprite in allSprites)
        {
            if (!sprite.enabled || !sprite.gameObject.activeInHierarchy)
                continue;

            var partName = sprite.transform.name.ToLowerInvariant();
            if (partName == "body" || partName.StartsWith("leg"))
                coreSprites.Add(sprite);
        }

        if (coreSprites.Count > 0)
            return coreSprites.ToArray();

        var visibleSprites = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (var sprite in allSprites)
        {
            if (sprite.enabled && sprite.gameObject.activeInHierarchy)
                visibleSprites.Add(sprite);
        }

        return visibleSprites.Count > 0 ? visibleSprites.ToArray() : allSprites;
    }

    private void Update()
    {
        moveInput = new Vector2(
            (Pressed(KeyCode.D, Key.D) || Pressed(KeyCode.RightArrow, Key.RightArrow) ? 1f : 0f) -
            (Pressed(KeyCode.A, Key.A) || Pressed(KeyCode.LeftArrow, Key.LeftArrow) ? 1f : 0f),
            (Pressed(KeyCode.W, Key.W) || Pressed(KeyCode.UpArrow, Key.UpArrow) ? 1f : 0f) -
            (Pressed(KeyCode.S, Key.S) || Pressed(KeyCode.DownArrow, Key.DownArrow) ? 1f : 0f));
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    private static bool Pressed(KeyCode legacyKey, Key inputKey)
    {
        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        pressed = Keyboard.current != null && Keyboard.current[inputKey].isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        pressed = pressed || Input.GetKey(legacyKey);
#endif
        return pressed;
    }

    private void FixedUpdate()
    {
        var view = Camera.main;
        Vector3 screenDirection = view == null
            ? new Vector3(moveInput.x, moveInput.y, 0f)
            : view.transform.right * moveInput.x + view.transform.up * moveInput.y;
        screenDirection.z = 0f;
        Vector2 gridDirection = new Vector2(screenDirection.x, screenDirection.y);
        if (gridDirection.sqrMagnitude > 0f)
            gridDirection.Normalize();
        body.MovePosition(body.position + gridDirection * moveSpeed * Time.fixedDeltaTime);
    }
}

