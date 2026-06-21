using UnityEngine;
#if UNITY_RENDER_PIPELINES_UNIVERSAL
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

// Todos los efectos visuales del Surge (burst, aura, trail y post-processing).
// Se adjunta a la esfera del jugador; solo escucha AbyssEvents, no conoce a
// SurgeManager ni a ningun otro sistema de gameplay.
public class SurgeVFXController : MonoBehaviour
{
    [Header("Color")]
    [SerializeField] Color primaryColor = new Color(1f, 0.65f, 0.1f);
    [SerializeField] Color secondaryColor = new Color(1f, 0.9f, 0.3f);

    [Header("Toggles de calidad (mobile gama baja)")]
    [SerializeField] bool enableParticles = true;
    [SerializeField] bool enablePostProcess = true;

    [Header("Burst (al activar Surge)")]
    [SerializeField] int burstCount = 60;
    [SerializeField] float burstSpeed = 4f;
    [SerializeField] float burstLifetime = 0.6f;
    [SerializeField] float burstSize = 0.25f;

    [Header("Aura (durante el Surge)")]
    [SerializeField] float auraEmissionRate = 25f;
    [SerializeField] float auraLifetime = 0.8f;
    [SerializeField] float auraSize = 0.2f;
    [SerializeField] float auraRadius = 0.6f;

    [Header("Trail de la esfera")]
    [SerializeField] float trailTime = 0.3f;
    [SerializeField] float trailStartWidth = 0.3f;

#if UNITY_RENDER_PIPELINES_UNIVERSAL
    [Header("Post-processing (URP Volume)")]
    [SerializeField] Volume ppVolume;
    [SerializeField] float bloomIntensityBoost = 6f;
    [SerializeField] float chromaticAberrationBoost = 0.4f;
    [SerializeField] float vignetteIntensityBoost = 0.25f;
    [SerializeField] float postProcessTransitionTime = 0.4f;
#endif

    const int BurstMaxParticles = 80;
    const int AuraMaxParticles = 60;

    ParticleSystem burstSystem;
    ParticleSystem auraSystem;
    TrailRenderer  trailRenderer;
    bool surging;

#if UNITY_RENDER_PIPELINES_UNIVERSAL
    Bloom bloom;
    ChromaticAberration chromaticAberration;
    Vignette vignette;
    float baseBloomIntensity, baseChromaticIntensity, baseVignetteIntensity;
    float ppBlend;
    bool ppReady;
#endif

    void Awake()
    {
        if (enableParticles)
        {
            var particleMaterial = BuildParticleMaterial(primaryColor);
            BuildBurstSystem(particleMaterial);
            BuildAuraSystem(particleMaterial);
            BuildTrail(particleMaterial);
        }

#if UNITY_RENDER_PIPELINES_UNIVERSAL
        CachePostProcessOverrides();
#endif
    }

    void OnEnable()
    {
        AbyssEvents.OnSurgeActivated   += HandleSurgeActivated;
        AbyssEvents.OnSurgeDeactivated += HandleSurgeDeactivated;
    }

    void OnDisable()
    {
        AbyssEvents.OnSurgeActivated   -= HandleSurgeActivated;
        AbyssEvents.OnSurgeDeactivated -= HandleSurgeDeactivated;
    }

    void Update()
    {
#if UNITY_RENDER_PIPELINES_UNIVERSAL
        UpdatePostProcess();
#endif
    }

    // ── Eventos ──────────────────────────────────────────────
    void HandleSurgeActivated()
    {
        surging = true;
        if (enableParticles)
        {
            burstSystem.Play();
            auraSystem.Play();
            trailRenderer.emitting = true;
        }
    }

    void HandleSurgeDeactivated()
    {
        surging = false;
        if (enableParticles)
        {
            auraSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            trailRenderer.emitting = false;
        }
    }

    // ── Particulas ───────────────────────────────────────────
    void BuildBurstSystem(Material material)
    {
        var go = new GameObject("SurgeBurstVFX");
        go.transform.SetParent(transform, false);
        burstSystem = go.AddComponent<ParticleSystem>();
        // AddComponent crea el sistema con playOnAwake=true por defecto y lo
        // arranca de inmediato; hay que detenerlo antes de poder tocar
        // "duration" (Unity lo prohibe mientras esta reproduciendose).
        burstSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = burstSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = burstLifetime;
        main.startLifetime = burstLifetime;
        main.startSpeed = burstSpeed;
        main.startSize = burstSize;
        main.startColor = primaryColor;
        main.maxParticles = BurstMaxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = burstSystem.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

        var shape = burstSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        var colorOverLifetime = burstSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = BuildFadeGradient(primaryColor, secondaryColor);

        // Stretch + velocityScale alargan cada particula en su direccion de
        // movimiento, dando el aspecto de chispa en vez de punto redondo.
        var psRenderer = burstSystem.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        psRenderer.lengthScale = 2f;
        psRenderer.velocityScale = 0.3f;
        psRenderer.material = material;
    }

    void BuildAuraSystem(Material material)
    {
        var go = new GameObject("SurgeAuraVFX");
        go.transform.SetParent(transform, false);
        auraSystem = go.AddComponent<ParticleSystem>();
        // Mismo motivo que en BuildBurstSystem: sin este Stop(), el aura
        // emitiria desde el arranque de la escena en vez de esperar al Surge.
        auraSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = auraSystem.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = auraLifetime;
        main.startSpeed = 0.5f;
        main.startSize = auraSize;
        main.startColor = secondaryColor;
        main.maxParticles = AuraMaxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = auraSystem.emission;
        emission.rateOverTime = auraEmissionRate;

        var shape = auraSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = auraRadius;

        var colorOverLifetime = auraSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = BuildFadeGradient(secondaryColor, primaryColor);

        var psRenderer = auraSystem.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.material = material;
    }

    void BuildTrail(Material material)
    {
        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null) trailRenderer = gameObject.AddComponent<TrailRenderer>();

        trailRenderer.time = trailTime;
        trailRenderer.startWidth = trailStartWidth;
        trailRenderer.endWidth = 0f;
        trailRenderer.colorGradient = BuildFadeGradient(primaryColor, secondaryColor);
        trailRenderer.material = material;
        trailRenderer.emitting = false;
        trailRenderer.generateLightingData = false;
    }

    static Gradient BuildFadeGradient(Color from, Color to)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        return gradient;
    }

    // Shader unlit de particulas de URP; si el proyecto no lo tiene disponible
    // cae a Sprites/Default, el unico shader fijo garantizado bajo cualquier
    // pipeline. Sprites/Default no expone _SrcBlend/_DstBlend como propiedades
    // (el blend queda fijo en el shader), asi que el fallback no logra additive
    // real, solo alpha blend normal.
    static Material BuildParticleMaterial(Color baseColor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        bool isUrpShader = shader != null;
        if (shader == null) shader = Shader.Find("Sprites/Default");

        var material = new Material(shader) { color = baseColor };

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

    // ── Post-processing ──────────────────────────────────────
#if UNITY_RENDER_PIPELINES_UNIVERSAL
    // Este bloque entero (incluido ppVolume en el Inspector) solo existe si
    // UNITY_RENDER_PIPELINES_UNIVERSAL esta definido en Player Settings >
    // Scripting Define Symbols.
    void CachePostProcessOverrides()
    {
        if (ppVolume == null || ppVolume.profile == null) return;

        ppVolume.profile.TryGet(out bloom);
        ppVolume.profile.TryGet(out chromaticAberration);
        ppVolume.profile.TryGet(out vignette);

        if (bloom != null) baseBloomIntensity = bloom.intensity.value;
        if (chromaticAberration != null) baseChromaticIntensity = chromaticAberration.intensity.value;
        if (vignette != null) baseVignetteIntensity = vignette.intensity.value;

        ppReady = bloom != null || chromaticAberration != null || vignette != null;
    }

    void UpdatePostProcess()
    {
        if (!enablePostProcess || !ppReady) return;

        float target = surging ? 1f : 0f;
        ppBlend = Mathf.MoveTowards(ppBlend, target, Time.deltaTime / Mathf.Max(0.01f, postProcessTransitionTime));

        if (bloom != null) bloom.intensity.value = baseBloomIntensity + bloomIntensityBoost * ppBlend;
        if (chromaticAberration != null) chromaticAberration.intensity.value = baseChromaticIntensity + chromaticAberrationBoost * ppBlend;
        if (vignette != null) vignette.intensity.value = baseVignetteIntensity + vignetteIntensityBoost * ppBlend;
    }
#endif
}
