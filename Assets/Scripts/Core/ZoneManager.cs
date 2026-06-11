using System.Collections;
using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    public enum Zone { Normal, Lava, Ice, Void }

    public static ZoneManager Instance { get; private set; }
    public Zone CurrentZone { get; private set; }

    [SerializeField] int ringsPerZone = 15;

    [Header("Colores de fondo por zona (estratos del pozo)")]
    [SerializeField] Color normalColor = new Color(0.1019608f, 0.0862745f, 0.0666667f); // Tierra
    [SerializeField] Color lavaColor   = new Color(0.1686275f, 0.0549020f, 0.0156863f); // Magma
    [SerializeField] Color iceColor    = new Color(0.0470588f, 0.1176471f, 0.1490196f); // Glaciar
    [SerializeField] Color voidColor   = new Color(0.0823529f, 0.0627451f, 0.1215686f); // Abismo

    static readonly Zone[] Cycle = { Zone.Normal, Zone.Lava, Zone.Ice, Zone.Void };

    int ringsPassed;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        ApplyZoneVisual();
    }

    public void NotifyRingPassed()
    {
        ringsPassed++;

        // El tutorial cuenta como zona Normal y no entra en el ciclo de zonas
        int tutorialRings = HelixGenerator.Instance != null ? HelixGenerator.Instance.TutorialRingCount : 0;
        int cycleRings    = Mathf.Max(0, ringsPassed - tutorialRings);

        int idx     = (cycleRings / ringsPerZone) % Cycle.Length;
        var newZone = Cycle[idx];

        if (newZone == CurrentZone) return;

        CurrentZone = newZone;
        ApplyZoneVisual();
        UIManager.Instance?.ShowZoneAnnouncement(ZoneName(CurrentZone));
    }

    void ApplyZoneVisual()
    {
        StartCoroutine(FadeBackground(ColorForZone(CurrentZone)));
    }

    IEnumerator FadeBackground(Color target)
    {
        if (Camera.main == null) yield break;
        Color start = Camera.main.backgroundColor;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f;
            Camera.main.backgroundColor = Color.Lerp(start, target, t);
            yield return null;
        }
    }

    Color ColorForZone(Zone z) => z switch
    {
        Zone.Lava  => lavaColor,
        Zone.Ice   => iceColor,
        Zone.Void  => voidColor,
        _          => normalColor,
    };

    static string ZoneName(Zone z) => z switch
    {
        Zone.Lava  => "ZONA LAVA",
        Zone.Ice   => "ZONA HIELO",
        Zone.Void  => "ZONA VACIO",
        _          => "ZONA NORMAL",
    };
}
