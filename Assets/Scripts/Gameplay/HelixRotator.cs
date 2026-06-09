using UnityEngine;
using UnityEngine.InputSystem;

public class HelixRotator : MonoBehaviour
{
    [SerializeField] float sensitivity   = 2.5f;
    [SerializeField] float inertiaDecay  = 5f;   // velocidad de frenado en zona Ice

    float lastTouchX;
    bool  isTouching;
    float angularVelocity; // grados/segundo; solo usado en zona Ice

    void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.State.Playing) return;

        var pointer = Pointer.current;
        if (pointer == null) return;

        bool isIce   = ZoneManager.Instance != null && ZoneManager.Instance.CurrentZone == ZoneManager.Zone.Ice;
        float currentX = pointer.position.ReadValue().x;

        if (pointer.press.wasPressedThisFrame)
        {
            lastTouchX = currentX;
            isTouching = true;
        }

        if (isTouching && pointer.press.isPressed)
        {
            float delta    = (currentX - lastTouchX) / Screen.width;
            lastTouchX     = currentX;
            float rotation = -delta * 360f * sensitivity;

            transform.Rotate(0f, rotation, 0f);

            // Guardar velocidad instantanea solo si estamos en Ice
            if (isIce && Time.deltaTime > 0f)
                angularVelocity = rotation / Time.deltaTime;
        }
        else if (isIce && Mathf.Abs(angularVelocity) > 0.5f)
        {
            // Inercia: continua girando y desacelera
            transform.Rotate(0f, angularVelocity * Time.deltaTime, 0f);
            angularVelocity = Mathf.Lerp(angularVelocity, 0f, inertiaDecay * Time.deltaTime);
        }
        else if (!isIce)
        {
            angularVelocity = 0f;
        }

        if (pointer.press.wasReleasedThisFrame)
            isTouching = false;
    }
}
