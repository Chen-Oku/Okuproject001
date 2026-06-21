using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Anillo de Surge en world space que rodea a la bola: refleja el medidor de
// SurgeManager via AbyssEvents. El seguimiento y el billboard corren en
// BallUIFollower (mismo GameObject); este script solo pinta el anillo y
// reacciona a los eventos, no conoce a SurgeManager.
public class SurgeMeterUI : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Image trackImage;
    [SerializeField] Image fillImage;

    [Header("Forma del anillo (sprite generado en runtime, sin assets)")]
    [SerializeField] int ringResolution = 128;
    [Tooltip("Radio interior del trazo como fraccion del radio exterior (0-1). Mas alto = trazo mas fino.")]
    [SerializeField] float innerRadiusRatio = 0.72f;

    [Header("Colores (interpolados sobre el medidor 0-1)")]
    [SerializeField] Color colorEmpty = Color.white;
    [SerializeField] Color colorMid   = new Color(1f, 0.878f, 0.227f); // #FFE03A
    [SerializeField] Color colorFull  = new Color(1f, 0.549f, 0f);     // #FF8C00
    [SerializeField] Color trackColor = new Color(0f, 0f, 0f, 0.25f);

    [Header("Timing")]
    [SerializeField] float fadeTime           = 0.2f;
    [SerializeField] float activatedFlashTime = 0.05f;
    [SerializeField] float activatedFadeTime  = 0.15f;

    // Compartido entre instancias: no hay razon para regenerar la textura por anillo.
    static Sprite ringSprite;

    Coroutine alphaRoutine;
    Coroutine activateRoutine;

    void Awake()
    {
        if (ringSprite == null) ringSprite = BuildRingSprite(ringResolution, innerRadiusRatio);

        trackImage.sprite = ringSprite;
        trackImage.type   = Image.Type.Simple;
        trackImage.color  = trackColor;

        fillImage.sprite       = ringSprite;
        fillImage.type         = Image.Type.Filled;
        fillImage.fillMethod   = Image.FillMethod.Radial360;
        fillImage.fillOrigin   = (int)Image.Origin360.Top;
        fillImage.fillClockwise = true;
        fillImage.fillAmount   = 0f;
        fillImage.color        = colorEmpty;

        canvasGroup.alpha = 0f;
    }

    void OnEnable()
    {
        AbyssEvents.OnSurgeMeterChanged += HandleMeterChanged;
        AbyssEvents.OnSurgeActivated   += HandleSurgeActivated;
        AbyssEvents.OnSurgeDeactivated += HandleSurgeDeactivated;
    }

    void OnDisable()
    {
        AbyssEvents.OnSurgeMeterChanged -= HandleMeterChanged;
        AbyssEvents.OnSurgeActivated   -= HandleSurgeActivated;
        AbyssEvents.OnSurgeDeactivated -= HandleSurgeDeactivated;
    }

    void HandleMeterChanged(float t)
    {
        fillImage.fillAmount = t;
        fillImage.color = t < 0.5f
            ? Color.Lerp(colorEmpty, colorMid, t / 0.5f)
            : Color.Lerp(colorMid, colorFull, (t - 0.5f) / 0.5f);

        FadeTo(t > 0.05f ? 1f : 0f);
    }

    void HandleSurgeActivated()
    {
        if (alphaRoutine != null) StopCoroutine(alphaRoutine);
        if (activateRoutine != null) StopCoroutine(activateRoutine);
        activateRoutine = StartCoroutine(FlashAndFadeOut());
    }

    void HandleSurgeDeactivated()
    {
        if (alphaRoutine != null) StopCoroutine(alphaRoutine);
        if (activateRoutine != null) StopCoroutine(activateRoutine);
        alphaRoutine = null;
        activateRoutine = null;

        fillImage.fillAmount = 0f;
        fillImage.color = colorEmpty;
        canvasGroup.alpha = 0f;
    }

    IEnumerator FlashAndFadeOut()
    {
        canvasGroup.alpha = 1f;
        fillImage.color = Color.white;
        fillImage.fillAmount = 1f;

        yield return new WaitForSeconds(activatedFlashTime);

        float t = 0f;
        while (t < activatedFadeTime)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / activatedFadeTime);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        fillImage.fillAmount = 0f;
        activateRoutine = null;
    }

    void FadeTo(float target)
    {
        if (alphaRoutine != null) StopCoroutine(alphaRoutine);
        alphaRoutine = StartCoroutine(FadeAlpha(target));
    }

    IEnumerator FadeAlpha(float target)
    {
        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, t / fadeTime);
            yield return null;
        }
        canvasGroup.alpha = target;
        alphaRoutine = null;
    }

    // Genera una textura "dona" (centro transparente, trazo solido en el borde)
    // para no depender de sprites externos; el Image.Type.Filled la recorta en
    // forma de arco sin tapar nunca el centro, a diferencia de un disco solido.
    static Sprite BuildRingSprite(int resolution, float innerRadiusRatio)
    {
        var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float center = resolution * 0.5f;
        float outerRadius = center;
        float innerRadius = outerRadius * innerRadiusRatio;
        const float antialias = 1.5f;

        var pixels = new Color32[resolution * resolution];
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float outerAlpha = Mathf.Clamp01((outerRadius - dist) / antialias);
                float innerAlpha = Mathf.Clamp01((dist - innerRadius) / antialias);
                byte alpha = (byte)(Mathf.Min(outerAlpha, innerAlpha) * 255f);

                pixels[y * resolution + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, resolution, resolution), new Vector2(0.5f, 0.5f));
    }
}
