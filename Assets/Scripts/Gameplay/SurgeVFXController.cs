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
    [SerializeField] float auraSizeMin = 0.12f;
    [SerializeField] float auraSizeMax = 0.3f;
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
            // El sprite circular solo va en burst/aura: el TrailRenderer
            // estira la textura a lo largo de toda la cinta, asi que con un
            // circulo se veria como una lente en vez de una linea continua.
            var particleMaterial = BuildParticleMaterial(primaryColor, BuildSoftCircleTexture());
            BuildBurstSystem(particleMaterial);
            BuildAuraSystem(particleMaterial);
            BuildTrail(BuildParticleMaterial(primaryColor));
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
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.45f);
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

        // Crecen un poco al nacer y luego encogen hasta desaparecer, en vez
        // de mantener un tamano fijo durante toda la vida.
        var sizeOverLifetime = burstSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = BuildGrowShrinkSizeCurve();

        // Billboard deja las particulas como circulos/esferas; lengthScale y
        // velocityScale solo aplican en modo Stretch, por eso ya no se usan.
        var psRenderer = burstSystem.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
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
        main.startSize = new ParticleSystem.MinMaxCurve(auraSizeMin, auraSizeMax);
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

        // Mismo crecimiento/encogido que el burst: sin esto cada particula
        // del aura mantiene su tamano random fijo desde que nace hasta que
        // muere, en vez de variar a lo largo de su vida.
        var sizeOverLifetime = auraSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = BuildGrowShrinkSizeCurve();

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

    // Curva de tamano compartida por burst y aura: nace al 60%, crece al
    // maximo a un 20% de su vida y se encoge hasta 0 antes de morir.
    static ParticleSystem.MinMaxCurve BuildGrowShrinkSizeCurve()
    {
        var curve = new AnimationCurve(
            new Keyframe(0f, 0.6f),
            new Keyframe(0.2f, 1.0f),
            new Keyframe(1f, 0.0f));
        return new ParticleSystem.MinMaxCurve(1f, curve);
    }

    const int CircleTextureSize = 32;
    static Texture2D circleTexture;

    // Sprite circular con caida de alfa hacia el borde. Sin esta textura el
    // ParticleSystemRenderer en modo Billboard pinta el quad completo y las
    // particulas se ven cuadradas en vez de redondas.
    static Texture2D BuildSoftCircleTexture()
    {
        if (circleTexture != null) return circleTexture;

        circleTexture = new Texture2D(CircleTextureSize, CircleTextureSize, TextureFormat.RGBA32, false);
        circleTexture.wrapMode = TextureWrapMode.Clamp;

        var center = new Vector2((CircleTextureSize - 1) * 0.5f, (CircleTextureSize - 1) * 0.5f);
        var pixels = new Color32[CircleTextureSize * CircleTextureSize];
        for (int y = 0; y < CircleTextureSize; y++)
        {
            for (int x = 0; x < CircleTextureSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / center.magnitude;
                float alpha = Mathf.Clamp01(1f - dist);
                pixels[y * CircleTextureSize + x] = new Color(1f, 1f, 1f, alpha * alpha);
            }
        }
        circleTexture.SetPixels32(pixels);
        circleTexture.Apply();
        return circleTexture;
    }

    // Shader unlit de particulas de URP; si el proyecto no lo tiene disponible
    // cae a Sprites/Default, el unico shader fijo garantizado bajo cualquier
    // pipeline. Sprites/Default no expone _SrcBlend/_DstBlend como propiedades
    // (el blend queda fijo en el shader), asi que el fallback no logra additive
    // real, solo alpha blend normal.
    static Material BuildParticleMaterial(Color baseColor, Texture2D sprite = null)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        bool isUrpShader = shader != null;
        if (shader == null) shader = Shader.Find("Sprites/Default");

        var material = new Material(shader) { color = baseColor };
        if (sprite != null) material.mainTexture = sprite;

        if (isUrpShader)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            // SrcAlpha en vez de One: con blend One/One el alfa del sprite
            // (la caida circular) no afecta el resultado y el additive pinta
            // el quad entero, por eso se veian cuadradas pese a la textura.
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
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
