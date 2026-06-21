using UnityEngine;

// Hace que este GameObject (y el Canvas World Space que lleve encima) siga a
// la bola y mire siempre a la camara, como un globo de comic. Solo toca su
// propio transform: no conoce a SurgeMeterUI ni a ComboBubble.
public class BallUIFollower : MonoBehaviour
{
    [SerializeField] Transform ballTransform;
    [SerializeField] Vector3 offset = new Vector3(0f, 1.5f, 0f);
    [Tooltip("Si se deja vacio, usa Camera.main.")]
    [SerializeField] Camera billboardCamera;

    void LateUpdate()
    {
        if (ballTransform == null) return;
        transform.position = ballTransform.position + offset;

        var cam = billboardCamera != null ? billboardCamera : Camera.main;
        if (cam == null) return;

        // El Canvas UI muestra su cara legible hacia -Z local; por eso se
        // apunta al punto OPUESTO a la camara (no a la camara), para que esa
        // cara quede de frente al jugador en vez de mostrarse espejada.
        // Billboard solo en Y: se descarta la inclinacion en X/Z despues del LookAt.
        Vector3 awayFromCamera = transform.position + (transform.position - cam.transform.position);
        transform.LookAt(awayFromCamera);
        transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
    }
}
