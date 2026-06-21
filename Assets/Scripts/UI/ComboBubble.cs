using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Globo individual del pool de combo: vuela desde el centro (la bola) hasta un
// punto aleatorio del arco superior, hace punch de escala y se apaga sola.
// Vive dentro del Canvas World Space que ya sigue a la bola (BallUIFollower),
// por eso trabaja solo en espacio LOCAL (anchoredPosition) y no conoce la
// posicion de la bola en el mundo. ComboBubblePool decide cuando llamar a Show().
public class ComboBubble : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI comboText;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] RectTransform tailRect;

    [Header("Arco (grados desde arriba; el resto cubre la franja inferior)")]
    [SerializeField] float arcHalfAngle = 150f;
    [Tooltip("Unidades locales del Canvas (no world): este Canvas ya esta escalado x0.01.")]
    [SerializeField] float arcRadius = 120f;

    [Header("Colores por tier (rango real 1-10 de ScoreManager.MaxCombo)")]
    [SerializeField] Color tierLowColor  = Color.white;
    [SerializeField] Color tierMidColor  = new Color(1f,    0.878f, 0.227f); // #FFE03A
    [SerializeField] Color tierHighColor = new Color(1f,    0.549f, 0f);    // #FF8C00
    [SerializeField] Color tierMaxColor  = new Color(1f,    0.188f, 0.188f);// #FF3030
    [SerializeField] Color tailColor     = new Color(0.04f, 0.04f, 0.08f, 0.85f);

    [Header("Timing")]
    [SerializeField] float flyDuration     = 0.15f;
    [SerializeField] float scaleUpTime     = 0.12f;
    [SerializeField] float scaleSettleTime = 0.08f;
    [SerializeField] float displayDuration = 1.0f;
    [SerializeField] float fadeDuration    = 0.25f;

    RectTransform rect;
    Coroutine routine;
    float totalLifetime;
    float elapsed;

    // Usado por ComboBubblePool para elegir la instancia mas vieja cuando todas estan ocupadas.
    public float TimeRemaining => gameObject.activeSelf ? Mathf.Max(0f, totalLifetime - elapsed) : 0f;

    void Awake()
    {
        rect = (RectTransform)transform;
        if (tailRect == null) tailRect = CreateTail();
    }

    public void Show(int tier)
    {
        if (routine != null) StopCoroutine(routine);

        comboText.text = $"×{tier}";
        comboText.color = ColorForTier(tier);

        gameObject.SetActive(true);
        routine = StartCoroutine(FlyPunchAndFade());
    }

    Color ColorForTier(int tier)
    {
        if (tier <= 2) return tierLowColor;
        if (tier <= 5) return tierMidColor;
        if (tier <= 9) return tierHighColor;
        return tierMaxColor;
    }

    IEnumerator FlyPunchAndFade()
    {
        float inDuration = Mathf.Max(flyDuration, scaleUpTime + scaleSettleTime);
        totalLifetime = inDuration + displayDuration + fadeDuration;
        elapsed = 0f;

        Vector2 target = RandomArcTarget();
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.zero;
        canvasGroup.alpha = 1f;
        UpdateTail(Vector2.zero);

        while (elapsed < totalLifetime)
        {
            elapsed += Time.deltaTime;

            if (elapsed <= inDuration)
            {
                float flyT = Mathf.Clamp01(elapsed / flyDuration);
                Vector2 pos = Vector2.Lerp(Vector2.zero, target, flyT);
                rect.anchoredPosition = pos;
                rect.localScale = Vector3.one * PunchScale(elapsed);
                UpdateTail(pos);
            }
            else if (elapsed > inDuration + displayDuration)
            {
                float fadeT = (elapsed - inDuration - displayDuration) / fadeDuration;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(fadeT));
            }

            yield return null;
        }

        canvasGroup.alpha = 0f;
        routine = null;
        gameObject.SetActive(false);
    }

    float PunchScale(float t)
    {
        if (t < scaleUpTime) return Mathf.Lerp(0f, 1.25f, t / scaleUpTime);
        return Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((t - scaleUpTime) / scaleSettleTime));
    }

    // angulo medido desde "arriba" (0 = arriba); el arco excluye la franja
    // directamente debajo de la bola para no tapar la caida.
    Vector2 RandomArcTarget()
    {
        float angle = Random.Range(-arcHalfAngle, arcHalfAngle) * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
        return dir * arcRadius;
    }

    void UpdateTail(Vector2 bubbleLocalPos)
    {
        Vector2 towardBall = -bubbleLocalPos;
        if (towardBall.sqrMagnitude < 0.0001f) return;
        float angleDeg = Vector2.SignedAngle(Vector2.up, towardBall);
        tailRect.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
    }

    // Sin sprite triangular disponible: un cuadrado chico rotado hace de cola.
    RectTransform CreateTail()
    {
        var go = new GameObject("Tail", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(rect, false);

        var image = go.GetComponent<Image>();
        image.color = tailColor;
        image.raycastTarget = false;

        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(8f, 8f);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }
}
