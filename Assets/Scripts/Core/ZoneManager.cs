using System.Collections;
using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    public enum Zone { Normal, Lava, Ice, Void }

    public static ZoneManager Instance { get; private set; }
    public Zone CurrentZone { get; private set; }

    [SerializeField] int ringsPerZone = 15;

    [Header("Colores de fondo por zona")]
    [SerializeField] Color normalColor = new Color(0.05f, 0.05f, 0.08f);
    [SerializeField] Color lavaColor   = new Color(0.15f, 0.02f, 0f);
    [SerializeField] Color iceColor    = new Color(0f,    0.05f, 0.15f);
    [SerializeField] Color voidColor   = new Color(0.05f, 0f,    0.12f);

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
        int idx     = (ringsPassed / ringsPerZone) % Cycle.Length;
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
