using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    public enum BallPowerup { None, Fire, Ghost, Slow }

    [SerializeField] float maxFallSpeed  = 15f;
    [SerializeField] float slowFallSpeed = 5f;

    Rigidbody rb;
    Renderer  rend;
    Color     originalColor;
    bool      isDead;

    Coroutine shieldRoutine;
    Coroutine powerupRoutine;

    public bool        HasShield     { get; private set; }
    public BallPowerup ActivePowerup { get; private set; }

    float ZoneSpeedMult => ZoneManager.Instance != null && ZoneManager.Instance.CurrentZone == ZoneManager.Zone.Void ? 1.5f : 1f;
    float CurrentMaxFallSpeed => (ActivePowerup == BallPowerup.Slow ? slowFallSpeed : maxFallSpeed) * ZoneSpeedMult;

    void Awake()
    {
        rb   = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
        if (rend != null) originalColor = rend.material.color;
    }

    void FixedUpdate()
    {
        if (isDead) return;
        if (rb.linearVelocity.y < -CurrentMaxFallSpeed)
            rb.linearVelocity = new Vector3(0f, -CurrentMaxFallSpeed, 0f);
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    // ── Combo ────────────────────────────────────────────────
    public bool TouchedSegmentThisRing { get; private set; }
    public void RegisterSegmentTouched() => TouchedSegmentThisRing = true;
    public void ResetSegmentTouched()    => TouchedSegmentThisRing = false;

    // ── Bounce / PassThrough ─────────────────────────────────
    public void Bounce(float upForce)
    {
        if (isDead) return;
        rb.linearVelocity = new Vector3(0f, upForce, 0f);
    }

    // Garantiza velocidad descendente para salir de la geometria de un segmento
    public void PassThrough()
    {
        if (isDead) return;
        if (rb.linearVelocity.y > -2f)
            rb.linearVelocity = new Vector3(0f, -2f, 0f);
    }

    // ── Shield (Checkpoint) ──────────────────────────────────
    public void ActivateShield()
    {
        if (isDead || HasShield) return;
        HasShield = true;
        if (rend != null)
        {
            if (shieldRoutine != null) StopCoroutine(shieldRoutine);
            shieldRoutine = StartCoroutine(ShieldPulse());
        }
    }

    IEnumerator ShieldPulse()
    {
        while (HasShield)
        {
            float t = Mathf.PingPong(Time.time * 3f, 1f);
            rend.material.color = Color.Lerp(originalColor, Color.cyan, t);
            yield return null;
        }
        RefreshColor();
    }

    // ── Powerups ─────────────────────────────────────────────
    public void ActivatePowerup(BallPowerup powerup, float duration)
    {
        if (isDead) return;
        if (powerupRoutine != null) StopCoroutine(powerupRoutine);
        powerupRoutine = StartCoroutine(PowerupRoutine(powerup, duration));
    }

    IEnumerator PowerupRoutine(BallPowerup powerup, float duration)
    {
        ActivePowerup = powerup;
        if (!HasShield) RefreshColor();
        yield return new WaitForSeconds(duration);
        ActivePowerup = BallPowerup.None;
        if (!HasShield) RefreshColor();
        powerupRoutine = null;
    }

    void RefreshColor()
    {
        if (rend == null) return;
        rend.material.color = ActivePowerup switch
        {
            BallPowerup.Fire  => new Color(1f,  0.45f, 0f),
            BallPowerup.Ghost => new Color(0.6f, 0f,   1f),
            BallPowerup.Slow  => new Color(0.4f, 0.8f, 1f),
            _                 => originalColor,
        };
    }

    // ── Death ────────────────────────────────────────────────
    public void Die()
    {
        if (isDead) return;

        if (HasShield)
        {
            HasShield = false;
            if (shieldRoutine != null) { StopCoroutine(shieldRoutine); shieldRoutine = null; }
            RefreshColor();
            return;
        }

        isDead = true;
        rb.isKinematic = true;
        GameManager.Instance.TriggerGameOver();
    }
}
