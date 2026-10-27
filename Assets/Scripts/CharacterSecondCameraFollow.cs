using UnityEngine;

/// <summary>Keeps the secondary gameplay camera centered on this character.</summary>
[DisallowMultipleComponent]
public sealed class CharacterSecondCameraFollow : MonoBehaviour
{
    [SerializeField] private Camera followCamera;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    private void LateUpdate()
    {
        if (followCamera == null)
            return;

        followCamera.transform.position = transform.position + offset;
    }
}
