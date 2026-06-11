using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 6f;
    // Camara fija en X/Z, inclinada hacia abajo: a menor yOffset, la bola sube en pantalla
    // y queda mas espacio visible debajo de ella (anillos proximos).
    [SerializeField] float yOffset = 2f;

    void LateUpdate()
    {
        if (target == null) return;

        float targetY = target.position.y + yOffset;
        float newY = Mathf.Lerp(transform.position.y, targetY, smoothSpeed * Time.deltaTime);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
