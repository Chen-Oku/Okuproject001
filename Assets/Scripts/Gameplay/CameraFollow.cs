using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 6f;
    [SerializeField] float yOffset = 8f;

    void LateUpdate()
    {
        if (target == null) return;

        float targetY = target.position.y + yOffset;
        float newY = Mathf.Lerp(transform.position.y, targetY, smoothSpeed * Time.deltaTime);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
