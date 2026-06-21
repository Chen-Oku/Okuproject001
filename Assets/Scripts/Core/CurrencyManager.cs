using System;
using UnityEngine;

// Moneda meta persistente ("Shards"): suscriptor puro de eventos ya existentes
// (profundidad, combo, surge). No conoce ni es conocido por ningun otro
// manager; la UI se conecta despues via OnShardsChanged.
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    public int Shards { get; private set; }

    // UI: se dispara en cada cambio de balance (Add y Spend).
    public static event Action<int /*newBalance*/> OnShardsChanged;

    [Header("Recompensas")]
    [SerializeField] int shardsPerZoneReached = 10;
    [Tooltip("Se multiplica por el nuevo record de combo de la run.")]
    [SerializeField] int shardsPerComboPoint = 2;
    [SerializeField] int shardsPerSurge = 25;

    const string ShardsKey = "Shards";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Shards = PlayerPrefs.GetInt(ShardsKey, 0);
    }

    void OnEnable()
    {
        ZoneManager.OnZoneReached    += HandleZoneReached;
        ScoreManager.OnNewComboMax   += HandleNewComboMax;
        AbyssEvents.OnSurgeActivated += HandleSurgeActivated;
    }

    void OnDisable()
    {
        ZoneManager.OnZoneReached    -= HandleZoneReached;
        ScoreManager.OnNewComboMax   -= HandleNewComboMax;
        AbyssEvents.OnSurgeActivated -= HandleSurgeActivated;
    }

    void HandleZoneReached(string zoneName, int depth) => Add(shardsPerZoneReached);
    void HandleNewComboMax(int newMax) => Add(newMax * shardsPerComboPoint);
    void HandleSurgeActivated() => Add(shardsPerSurge);

    public void Add(int amount)
    {
        if (amount <= 0) return;
        Shards += amount;
        Save();
    }

    public bool Spend(int amount)
    {
        if (amount <= 0 || amount > Shards) return false;
        Shards -= amount;
        Save();
        return true;
    }

    public int Get() => Shards;

    void Save()
    {
        PlayerPrefs.SetInt(ShardsKey, Shards);
        PlayerPrefs.Save();
        OnShardsChanged?.Invoke(Shards);
    }
}
