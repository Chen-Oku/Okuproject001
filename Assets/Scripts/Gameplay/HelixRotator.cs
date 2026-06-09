using UnityEngine;
using UnityEngine.InputSystem;

public class HelixRotator : MonoBehaviour
{
    [SerializeField] float sensitivity = 2.5f;

    float lastTouchX;
    bool isTouching;

    void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.State.Playing) return;

        var pointer = Pointer.current;
        if (pointer == null) return;

        float currentX = pointer.position.ReadValue().x;

        if (pointer.press.wasPressedThisFrame)
        {
            lastTouchX = currentX;
            isTouching = true;
        }
        else if (isTouching && pointer.press.isPressed)
        {
            float delta = (currentX - lastTouchX) / Screen.width;
            lastTouchX = currentX;
            transform.Rotate(0f, -delta * 360f * sensitivity, 0f);
        }

        if (pointer.press.wasReleasedThisFrame)
            isTouching = false;
    }
}
