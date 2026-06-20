using UnityEngine;

// Traduce combo y Surge en feedback de camara (FOV + shake). No conoce
// HelixGenerator ni la fisica del juego; solo escucha eventos existentes.
public class SpeedFeedbackManager : MonoBehaviour
{
    public static SpeedFeedbackManager Instance { get; private set; }

    [Header("Referencias (Main Camera)")]
    [SerializeField] Camera targetCamera;
    [SerializeField] CameraFollow cameraFollow;

    [Header("FOV por tier de combo")]
    [SerializeField] float baseFov = 60f;
    [SerializeField] float maxFov = 70f;
    [SerializeField] AnimationCurve comboFovCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("FOV por Surge")]
    [SerializeField] float surgeFovBoost = 8f;
    [SerializeField] AnimationCurve surgeBoostCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] float surgeTransitionTime = 0.6f;

    [Header("Suavizado de FOV")]
    [SerializeField] float fovSmoothTime = 0.35f;

    [Header("Shake - Impacto por tipo de segmento")]
    [SerializeField] float shakeAmplitudeSafe = 0f;
    [SerializeField] float shakeAmplitudeBouncy = 0.08f;
    [SerializeField] float shakeAmplitudeCrumbling = 0.12f;
    [SerializeField] float shakeAmplitudeCheckpoint = 0.05f;
    [SerializeField] float shakeAmplitudeDangerous = 0.35f;
    [SerializeField] float shakeAmplitudeFireLocked = 0.35f;

    [Header("Shake - Zona y Surge")]
    [SerializeField] float zoneShakeAmplitude = 0.15f;
    [SerializeField] float surgeShakeAmplitude = 0.2f;

    [Header("Shake - Global")]
    [SerializeField] float globalShakeMultiplier = 1f;
    [SerializeField] float shakeFrequency = 25f;
    [SerializeField] float traumaDecayPerSecond = 2.5f;

    [Header("Modo bajo rendimiento")]
    [SerializeField, Range(0f, 1f)] float lowEndIntensityScale = 0.4f;

    const string ReducedIntensityKey = "ReducedFeedbackIntensity";
    public bool ReducedIntensityEnabled { get; private set; }

    int   currentComboStreak;
    bool  surgeActive;
    float surgeBlend;
    float trauma;
    float currentFov;
    float fovVelocity;
    float noiseSeedX, noiseSeedY, noiseSeedZ;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        ReducedIntensityEnabled = PlayerPrefs.GetInt(ReducedIntensityKey, 0) == 1;

        noiseSeedX = Random.Range(0f, 1000f);
        noiseSeedY = Random.Range(0f, 1000f);
        noiseSeedZ = Random.Range(0f, 1000f);

        currentFov = baseFov;
        if (targetCamera != null) targetCamera.fieldOfView = baseFov;
    }

    void OnEnable()
    {
        ScoreManager.OnComboChanged  += HandleComboChanged;
        ZoneManager.OnZoneReached    += HandleZoneReached;
        RingSegment.OnSegmentImpact  += HandleSegmentImpact;
        GameManager.OnRunStart       += HandleRunStart;
        GameManager.OnRunEnd         += HandleRunEnd;
    }

    void OnDisable()
    {
        ScoreManager.OnComboChanged  -= HandleComboChanged;
        ZoneManager.OnZoneReached    -= HandleZoneReached;
        RingSegment.OnSegmentImpact  -= HandleSegmentImpact;
        GameManager.OnRunStart       -= HandleRunStart;
        GameManager.OnRunEnd         -= HandleRunEnd;
    }

    void Update()
    {
        UpdateShake();
        UpdateFov();
    }

    // ── Shake ────────────────────────────────────────────────
    // Modelo de "trauma" (Eiserloh): un solo valor 0..1 acumula impactos y
    // decae solo; el offset visual usa trauma^2 + ruido Perlin por eje para
    // un movimiento organico sin el flicker del ruido puramente aleatorio.
    void UpdateShake()
    {
        if (trauma > 0f)
            trauma = Mathf.Max(0f, trauma - traumaDecayPerSecond * Time.deltaTime);

        float shakeAmount = trauma * trauma;
        float t = Time.time * shakeFrequency;

        float x = Mathf.PerlinNoise(noiseSeedX, t) * 2f - 1f;
        float y = Mathf.PerlinNoise(noiseSeedY, t) * 2f - 1f;
        float z = Mathf.PerlinNoise(noiseSeedZ, t) * 2f - 1f;

        Vector3 offset = new Vector3(x, y, z) * shakeAmount * globalShakeMultiplier;
        if (cameraFollow != null) cameraFollow.ShakeOffset = offset;
    }

    void AddTrauma(float amount)
    {
        float scale = ReducedIntensityEnabled ? lowEndIntensityScale : 1f;
        trauma = Mathf.Clamp01(trauma + amount * scale);
    }

    // ── FOV ──────────────────────────────────────────────────
    // Se corre en Update (no LateUpdate) para garantizar que CameraFollow.LateUpdate
    // siempre lea un ShakeOffset ya actualizado este mismo frame, sin depender
    // del orden de ejecucion entre scripts distintos.
    void UpdateFov()
    {
        float comboNorm  = Mathf.Clamp01((float)currentComboStreak / ScoreManager.MaxCombo);
        float comboBlend = comboFovCurve.Evaluate(comboNorm);
        float comboFov   = Mathf.Lerp(baseFov, maxFov, comboBlend);

        float surgeTarget = surgeActive ? 1f : 0f;
        surgeBlend = Mathf.MoveTowards(surgeBlend, surgeTarget, Time.deltaTime / Mathf.Max(0.01f, surgeTransitionTime));
        float surgeBoost = surgeFovBoost * surgeBoostCurve.Evaluate(surgeBlend);

        float intensityScale = ReducedIntensityEnabled ? lowEndIntensityScale : 1f;
        float targetFov = baseFov + (comboFov - baseFov + surgeBoost) * intensityScale;

        currentFov = Mathf.SmoothDamp(currentFov, targetFov, ref fovVelocity, fovSmoothTime);
        if (targetCamera != null) targetCamera.fieldOfView = currentFov;
    }

    // ── Eventos ──────────────────────────────────────────────
    void HandleComboChanged(int streak) => currentComboStreak = streak;

    void HandleZoneReached(string zoneName, int depth) => AddTrauma(zoneShakeAmplitude);

    void HandleSegmentImpact(RingSegment.SegmentType type)
    {
        float amplitude = type switch
        {
            RingSegment.SegmentType.Bouncy     => shakeAmplitudeBouncy,
            RingSegment.SegmentType.Crumbling  => shakeAmplitudeCrumbling,
            RingSegment.SegmentType.Checkpoint => shakeAmplitudeCheckpoint,
            RingSegment.SegmentType.Dangerous  => shakeAmplitudeDangerous,
            RingSegment.SegmentType.FireLocked => shakeAmplitudeFireLocked,
            _                                  => shakeAmplitudeSafe,
        };
        AddTrauma(amplitude);
    }

    void HandleRunStart() => ResetState();
    void HandleRunEnd(int depth, string cause, float durationSeconds) => ResetState();

    void ResetState()
    {
        currentComboStreak = 0;
        surgeActive = false;
        surgeBlend = 0f;
        trauma = 0f;
        currentFov = baseFov;
        fovVelocity = 0f;
        if (targetCamera  != null) targetCamera.fieldOfView = baseFov;
        if (cameraFollow != null) cameraFollow.ShakeOffset = Vector3.zero;
    }

    // ── API publica ──────────────────────────────────────────
    public void SetReducedIntensity(bool enabled)
    {
        ReducedIntensityEnabled = enabled;
        PlayerPrefs.SetInt(ReducedIntensityKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Surge (mecanica V1.5 aun no implementada): un futuro SurgeSystem llamara
    // estos metodos directamente, igual que AnalyticsManager.LogSurgeActivated.
    // Los [ContextMenu] son solo para poder probarlos a mano en el Editor.
    [ContextMenu("Debug: Surge Start")]
    public void NotifySurgeStart()
    {
        surgeActive = true;
        AddTrauma(surgeShakeAmplitude);
    }

    [ContextMenu("Debug: Surge End")]
    public void NotifySurgeEnd() => surgeActive = false;
}
