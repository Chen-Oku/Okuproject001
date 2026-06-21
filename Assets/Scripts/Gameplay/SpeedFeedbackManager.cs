using System.Collections;
using UnityEngine;

// Hijo de la Main Camera. Traduce combo y Surge en feedback visual: FOV
// dinamico, camera shake y speed lines. El FOV/las speed lines NO se atan a
// la velocidad de caida (casi constante); se atan al tier de combo y al
// estado de Surge, que si varian con el riesgo asumido. Solo escucha
// AbyssEvents: no conoce HelixGenerator ni la fisica del juego.
public class SpeedFeedbackManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] Camera targetCamera;
    [SerializeField] CameraFollow cameraFollow;
    [SerializeField] Transform player;

    [Header("FOV por combo / Surge")]
    [SerializeField] float fovBase = 60f;
    [SerializeField] float fovMax = 88f;
    [SerializeField] float fovSurge = 90f;
    [SerializeField] float fovLerpSpeed = 6f;

    [Header("Camera shake")]
    [SerializeField] float impactShakeMagnitude = 0.3f;
    [SerializeField] float zoneShakeMagnitude = 0.15f;
    [SerializeField] float surgeShakeMagnitude = 0.25f;
    [SerializeField] float shakeDuration = 0.25f;

    [Header("Speed lines")]
    [SerializeField] bool enableSpeedLines = true;
    [SerializeField] float speedLinesRadius = 1.8f;
    [SerializeField] float speedLinesUpSpeed = 4f;
    [SerializeField] float speedLinesMaxEmission = 30f;
    [SerializeField] int maxParticles = 40;

    int currentComboTier;
    bool surgeActive;
    float fovTarget;

    Coroutine shakeCoroutine;
    ParticleSystem speedLines;
    ParticleSystem.EmissionModule speedLinesEmission;

    void Awake()
    {
        if (targetCamera == null) targetCamera = GetComponentInParent<Camera>();
        if (cameraFollow == null) cameraFollow = GetComponentInParent<CameraFollow>();

        fovTarget = fovBase;
        if (targetCamera != null) targetCamera.fieldOfView = fovBase;

        if (enableSpeedLines) BuildSpeedLines();
    }

    void OnEnable()
    {
        AbyssEvents.OnComboChanged     += HandleComboChanged;
        AbyssEvents.OnSurgeActivated   += HandleSurgeActivated;
        AbyssEvents.OnSurgeDeactivated += HandleSurgeDeactivated;
        AbyssEvents.OnImpact           += HandleImpact;
        AbyssEvents.OnZoneTransition   += HandleZoneTransition;
    }

    void OnDisable()
    {
        AbyssEvents.OnComboChanged     -= HandleComboChanged;
        AbyssEvents.OnSurgeActivated   -= HandleSurgeActivated;
        AbyssEvents.OnSurgeDeactivated -= HandleSurgeDeactivated;
        AbyssEvents.OnImpact           -= HandleImpact;
        AbyssEvents.OnZoneTransition   -= HandleZoneTransition;
    }

    void LateUpdate()
    {
        if (targetCamera != null)
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, fovTarget, fovLerpSpeed * Time.deltaTime);

        // Reposicionar el emisor sobre el jugador cada frame (en vez de
        // parentarlo) preserva el espacio LOCAL del particle system: las
        // particulas ya emitidas conservan su velocidad local hacia arriba,
        // dando la ilusion de estela aun cuando el emisor se mueve en mundo.
        if (enableSpeedLines && player != null && speedLines != null)
            speedLines.transform.position = player.position;
    }

    // ── FOV ──────────────────────────────────────────────────
    float ComboFov(int tier)
    {
        float t = Mathf.Clamp01((float)tier / ScoreManager.MaxCombo);
        return Mathf.Lerp(fovBase, fovMax, t);
    }

    // ── Speed lines ──────────────────────────────────────────
    float ComboEmission(int tier)
    {
        float t = Mathf.Clamp01((float)tier / ScoreManager.MaxCombo);
        return Mathf.Lerp(0f, speedLinesMaxEmission, t);
    }

    void UpdateSpeedLinesRate()
    {
        if (!enableSpeedLines) return;
        speedLinesEmission.rateOverTime = surgeActive ? speedLinesMaxEmission : ComboEmission(currentComboTier);
    }

    void BuildSpeedLines()
    {
        var go = new GameObject("SpeedLinesVFX");
        go.transform.SetParent(transform, false);
        speedLines = go.AddComponent<ParticleSystem>();
        // AddComponent arranca el sistema solo (playOnAwake=true por defecto);
        // hay que detenerlo antes de poder tocar simulationSpace/shape.
        speedLines.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = speedLines.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 0.6f;
        main.startSpeed = 0f;
        main.startSize = 0.05f;
        main.startColor = new Color(1f, 1f, 1f, 0.6f);
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var shape = speedLines.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = speedLinesRadius;
        // Thickness bajo: las particulas nacen cerca del borde del circulo
        // (anillo alrededor del jugador) en vez de rellenar todo el disco.
        shape.radiusThickness = 0.2f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var velocityOverLifetime = speedLines.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(speedLinesUpSpeed);

        speedLinesEmission = speedLines.emission;
        speedLinesEmission.rateOverTime = 0f;

        var psRenderer = speedLines.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        psRenderer.lengthScale = 4f;
        psRenderer.velocityScale = 0.5f;
        psRenderer.material = BuildSpeedLineMaterial();
    }

    // Mismo patron que SurgeVFXController: shader unlit de particulas URP,
    // con fallback a Sprites/Default si el proyecto no corre bajo URP.
    static Material BuildSpeedLineMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        bool isUrpShader = shader != null;
        if (shader == null) shader = Shader.Find("Sprites/Default");

        var material = new Material(shader) { color = Color.white };

        if (isUrpShader)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        return material;
    }

    // ── Shake ────────────────────────────────────────────────
    void StartShake(float magnitude)
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(magnitude, shakeDuration));
    }

    // El offset se escribe en CameraFollow.ShakeOffset (no en transform.local
    // Position propio): CameraFollow.LateUpdate sobreescribe la posicion de
    // la Main Camera todos los frames a partir de basePosition + ShakeOffset,
    // precisamente para que ningun otro script pelee por esa escritura. Un
    // offset puesto directo en localPosition aqui quedaria pisado al instante.
    IEnumerator ShakeRoutine(float magnitude, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float damper = 1f - Mathf.Clamp01(elapsed / duration);
            if (cameraFollow != null) cameraFollow.ShakeOffset = Random.insideUnitSphere * magnitude * damper;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (cameraFollow != null) cameraFollow.ShakeOffset = Vector3.zero;
        shakeCoroutine = null;
    }

    // ── Eventos ──────────────────────────────────────────────
    void HandleComboChanged(int tier)
    {
        currentComboTier = tier;
        if (!surgeActive) fovTarget = ComboFov(tier);
        UpdateSpeedLinesRate();
    }

    void HandleSurgeActivated()
    {
        surgeActive = true;
        fovTarget = fovSurge;
        UpdateSpeedLinesRate();
        StartShake(surgeShakeMagnitude);
    }

    void HandleSurgeDeactivated()
    {
        surgeActive = false;
        fovTarget = ComboFov(currentComboTier);
        UpdateSpeedLinesRate();
    }

    void HandleImpact(float intensity) => StartShake(impactShakeMagnitude * Mathf.Max(0f, intensity));

    void HandleZoneTransition() => StartShake(zoneShakeMagnitude);
}
