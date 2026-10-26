using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smooth = 6f;
    public float orthoSize = 7f;

    void Start()
    {
        var cam = GetComponent<Camera>();
        if (cam != null) cam.orthographicSize = orthoSize;
    }

    void LateUpdate()
    {
        if (target == null) return;
        var p = Vector3.Lerp(transform.position, new Vector3(target.position.x, target.position.y, transform.position.z), smooth * Time.deltaTime);
        transform.position = p;
    }
}
