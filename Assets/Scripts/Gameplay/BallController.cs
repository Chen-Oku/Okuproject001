using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    [SerializeField] float maxFallSpeed = 15f;

    Rigidbody rb;
    bool isDead;

    void Awake() => rb = GetComponent<Rigidbody>();

    void FixedUpdate()
    {
        if (isDead) return;

        // Clamp caida para evitar tunneling a alta velocidad
        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector3(0f, -maxFallSpeed, 0f);

        // Mantener bola centrada en X/Z (solo cae verticalmente)
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.isKinematic = true;
        GameManager.Instance.TriggerGameOver();
    }
}
