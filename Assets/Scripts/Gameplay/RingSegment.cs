using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class RingSegment : MonoBehaviour
{
    public enum SegmentType { Safe, Dangerous, Crumbling, Bouncy, Checkpoint, FireLocked }

    [SerializeField] float bounceForce  = 10f;
    [SerializeField] float crumbleDelay = 0.45f;

    public SegmentType Type { get; private set; }

    // Feedback de impacto (shake/FOV); no acopla este script a quien escuche.
    public static event Action<SegmentType> OnSegmentImpact;

    Renderer rend;
    bool consumed;

    void Awake() => rend = GetComponent<Renderer>();

    public void Setup(SegmentType type, Material mat)
    {
        Type = type;
        rend.material = mat;
    }

    void OnCollisionEnter(Collision col)
    {
        if (consumed) return;
        if (!col.gameObject.TryGetComponent<BallController>(out var ball)) return;

        OnSegmentImpact?.Invoke(Type);

        switch (Type)
        {
            case SegmentType.Dangerous:
                if (ball.ActivePowerup == BallController.BallPowerup.Ghost)
                {
                    // Desactivar fisica entre bola y este segmento para que lo atraviese
                    Physics.IgnoreCollision(col.collider, GetComponent<Collider>());
                    ball.PassThrough();
                    return;
                }
                ball.Die(Type.ToString());
                break;

            case SegmentType.Safe:
                // Fire: destruye el segmento al contacto (mantiene combo)
                if (ball.ActivePowerup == BallController.BallPowerup.Fire)
                    { consumed = true; Destroy(gameObject); return; }
                ball.RegisterSegmentTouched();
                break;

            case SegmentType.Crumbling:
                consumed = true;
                if (ball.ActivePowerup == BallController.BallPowerup.Fire)
                    { Destroy(gameObject); return; }
                ball.RegisterSegmentTouched();
                StartCoroutine(Crumble());
                break;

            case SegmentType.Bouncy:
                ball.RegisterSegmentTouched();
                ball.Bounce(bounceForce);
                break;

            case SegmentType.Checkpoint:
                consumed = true;
                ball.ActivateShield();
                gameObject.SetActive(false);
                break;

            case SegmentType.FireLocked:
                // Solo el powerup de fuego permite destruirlo y pasar
                if (ball.ActivePowerup == BallController.BallPowerup.Fire)
                    { consumed = true; Destroy(gameObject); return; }
                ball.Die(Type.ToString());
                break;
        }
    }

    IEnumerator Crumble()
    {
        // Feedback visual: oscurece el segmento mientras se rompe
        Color broken = new Color(0.3f, 0.15f, 0f);
        Color original = rend.material.color;
        float t = 0f;
        while (t < crumbleDelay)
        {
            t += Time.deltaTime;
            rend.material.color = Color.Lerp(original, broken, t / crumbleDelay);
            yield return null;
        }
        Destroy(gameObject);
    }
}
