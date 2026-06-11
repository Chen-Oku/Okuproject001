using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PowerupPickup : MonoBehaviour
{
    BallController.BallPowerup type;
    float     duration;
    Transform ballTransform;
    float     baseY;

    const float DespawnDistanceBelow = 12f;

    public void Setup(BallController.BallPowerup powerupType, float dur, Material mat, GameObject vfxPrefab, Transform ball)
    {
        type           = powerupType;
        duration       = dur;
        ballTransform  = ball;
        baseY          = transform.position.y;
        GetComponent<Renderer>().material = mat;

        if (vfxPrefab != null)
        {
            var vfx = Instantiate(vfxPrefab, transform);
            vfx.transform.localPosition = Vector3.zero;
        }
    }

    void Update()
    {
        // Spin sobre eje mundo Y
        transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);

        // Bob vertical suave para llamar la atencion
        float y = baseY + Mathf.Sin(Time.time * 2.5f) * 0.18f;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

        // Auto-destruir cuando el ball queda muy por debajo
        if (ballTransform != null && ballTransform.position.y < transform.position.y - DespawnDistanceBelow)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<BallController>(out var ball)) return;
        ball.ActivatePowerup(type, duration);
        Destroy(gameObject);
    }
}
