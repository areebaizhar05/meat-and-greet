using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerSpriteAnimator : MonoBehaviour
{
    private enum Facing { Front, SideLeft, SideRight, Back }

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TopDownPlayerMovement movement;
    [SerializeField, Min(1f)] private float framesPerSecond = 6f;
    [SerializeField] private Sprite[] frontFrames = new Sprite[4];
    [SerializeField] private Sprite[] sideLeftFrames = new Sprite[2];
    [SerializeField] private Sprite[] sideRightFrames = new Sprite[2];
    [SerializeField] private Sprite[] backFrames = new Sprite[4];

    private Facing facing = Facing.Front;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (movement == null)
            movement = GetComponent<TopDownPlayerMovement>();
    }

    private void Update()
    {
        if (spriteRenderer == null || movement == null)
            return;

        var input = movement.MoveInput;
        var moving = input.sqrMagnitude > 0.01f;
        if (moving)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                facing = input.x < 0f ? Facing.SideLeft : Facing.SideRight;
            else
                facing = input.y > 0f ? Facing.Back : Facing.Front;
        }

        var frames = FramesForFacing();
        if (frames == null || frames.Length == 0)
            return;

        var frameIndex = moving ? Mathf.FloorToInt(Time.time * framesPerSecond) % frames.Length : 0;
        if (frames[frameIndex] != null)
            spriteRenderer.sprite = frames[frameIndex];
    }

    private Sprite[] FramesForFacing()
    {
        switch (facing)
        {
            case Facing.SideLeft: return sideLeftFrames;
            case Facing.SideRight: return sideRightFrames;
            case Facing.Back: return backFrames;
            default: return frontFrames;
        }
    }
}
