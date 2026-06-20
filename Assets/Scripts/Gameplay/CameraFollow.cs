using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 6f;
    // Camara fija en X/Z, inclinada hacia abajo: a menor yOffset, la bola sube en pantalla
    // y queda mas espacio visible debajo de ella (anillos proximos).
    [SerializeField] float yOffset = 2f;

    // Offset aditivo para camera shake (SpeedFeedbackManager); se compone aqui
    // para que nadie mas pelee por escribir transform.position este frame.
    public Vector3 ShakeOffset { get; set; }

    // Posicion limpia sin shake. Nunca se lee desde transform.position: si lo
    // hicieramos, el ShakeOffset del frame anterior quedaria incluido y el
    // shake se acumularia sobre si mismo en vez de ser siempre transitorio.
    Vector3 basePosition;

    void Start() => basePosition = transform.position;

    void LateUpdate()
    {
        if (target == null) return;

        float targetY = target.position.y + yOffset;
        basePosition.y = Mathf.Lerp(basePosition.y, targetY, smoothSpeed * Time.deltaTime);
        transform.position = basePosition + ShakeOffset;
    }
}
