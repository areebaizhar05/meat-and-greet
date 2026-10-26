using UnityEngine;

/// <summary>Keeps an angled 2D camera centered on the moving player.</summary>
[DisallowMultipleComponent]
public sealed class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(16f, -7.2f, 20f);
    [SerializeField] private Vector3 targetOffset;

    private void LateUpdate()
    {
        if (target == null)
            return;

        var focusPosition = target.TransformPoint(targetOffset);
        transform.position = focusPosition + offset;
        var direction = focusPosition - transform.position;
        var up = Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > 0.98f
            ? Vector3.forward
            : Vector3.up;
        transform.rotation = Quaternion.LookRotation(direction, up);
    }
}
