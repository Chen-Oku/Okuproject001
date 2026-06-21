using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    public enum BallPowerup { None, Fire, Ghost, Slow }

    [SerializeField] float maxFallSpeed  = 15f;
    [SerializeField] float slowFallSpeed = 5f;

    [Header("Powerup VFX")]
    [SerializeField] GameObject firePowerupVFX;
    [SerializeField] GameObject ghostPowerupVFX;
    [SerializeField] GameObject slowPowerupVFX;

    Rigidbody rb;
    Renderer  rend;
    Color     originalColor;
    bool      isDead;

    Coroutine shieldRoutine;
    Coroutine powerupRoutine;

    GameObject fireVFX, ghostVFX, slowVFX;
    GameObject activeVFX;
    Quaternion activeVFXRestRotation = Quaternion.identity;
    bool       vfxFlippedUp;

    public bool        HasShield     { get; private set; }
    public BallPowerup ActivePowerup { get; private set; }

    float ZoneSpeedMult => ZoneManager.Instance != null && ZoneManager.Instance.CurrentZone == ZoneManager.Zone.Void ? 1.5f : 1f;
    float CurrentMaxFallSpeed => (ActivePowerup == BallPowerup.Slow ? slowFallSpeed : maxFallSpeed) * ZoneSpeedMult;

    void Awake()
    {
        rb   = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
        if (rend != null) originalColor = rend.material.color;

        fireVFX  = SpawnVFX(firePowerupVFX);
        ghostVFX = SpawnVFX(ghostPowerupVFX);
        slowVFX  = SpawnVFX(slowPowerupVFX);
    }

    GameObject SpawnVFX(GameObject prefab)
    {
        if (prefab == null) return null;
        var instance = Instantiate(prefab, transform);
        instance.transform.localPosition = Vector3.zero;
        instance.SetActive(false);
        return instance;
    }

    void FixedUpdate()
    {
        if (isDead || rb.isKinematic) return;
        if (rb.linearVelocity.y < -CurrentMaxFallSpeed)
            rb.linearVelocity = new Vector3(0f, -CurrentMaxFallSpeed, 0f);
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        UpdateVFXOrientation();
    }

    // Voltea el VFX activo 180° en X para que la emision siga el sentido vertical real del ball
    void UpdateVFXOrientation()
    {
        if (activeVFX == null) return;

        bool movingUp = rb.linearVelocity.y > 0f;
        if (movingUp == vfxFlippedUp) return;

        vfxFlippedUp = movingUp;
        activeVFX.transform.localRotation = movingUp
            ? activeVFXRestRotation * Quaternion.Euler(180f, 0f, 0f)
            : activeVFXRestRotation;
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
        SetActiveVFX(powerup);
        yield return new WaitForSeconds(duration);
        ActivePowerup = BallPowerup.None;
        if (!HasShield) RefreshColor();
        SetActiveVFX(BallPowerup.None);
        powerupRoutine = null;
    }

    void SetActiveVFX(BallPowerup powerup)
    {
        if (activeVFX != null)
        {
            activeVFX.transform.localRotation = activeVFXRestRotation;
            activeVFX.SetActive(false);
        }
        activeVFX = powerup switch
        {
            BallPowerup.Fire  => fireVFX,
            BallPowerup.Ghost => ghostVFX,
            BallPowerup.Slow  => slowVFX,
            _                 => null,
        };
        if (activeVFX != null)
        {
            activeVFXRestRotation = activeVFX.transform.localRotation;
            vfxFlippedUp = false;
            activeVFX.SetActive(true);
        }
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
    public void Die(string cause = "Unknown")
    {
        if (isDead) return;

        SurgeManager.Instance?.OnPlayerHit();
        AbyssEvents.TriggerImpact(0.7f);

        if (HasShield)
        {
            HasShield = false;
            if (shieldRoutine != null) { StopCoroutine(shieldRoutine); shieldRoutine = null; }
            RefreshColor();
            return;
        }

        isDead = true;
        rb.isKinematic = true;
        GameManager.Instance.TriggerGameOver(cause);
    }
}
