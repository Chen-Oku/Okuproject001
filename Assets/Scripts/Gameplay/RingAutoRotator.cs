using UnityEngine;

public class RingAutoRotator : MonoBehaviour
{
    float speed; // grados/segundo; negativo = sentido contrario

    public void Setup(float degreesPerSecond) => speed = degreesPerSecond;

    void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.State.Playing) return;
        transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.Self);
    }
}
