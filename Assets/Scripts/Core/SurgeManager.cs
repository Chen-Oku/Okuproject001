using UnityEngine;

// Medidor de "caida limpia": sube cuando otro sistema reporta un anillo cruzado
// sin golpe (OnRingSafeCleared) y drena solo con el tiempo, con un golpe extra
// si el jugador recibe daño (OnPlayerHit). Al llenarse activa Surge por N
// segundos. No conoce quien lo llama ni quien escucha: publica su estado via
// AbyssEvents y expone ScoreMultiplier para que ScoreManager lo aplique.
public class SurgeManager : MonoBehaviour
{
    public static SurgeManager Instance { get; private set; }

    [Header("Medidor")]
    [Tooltip("Cuanto sube el medidor (0-1) cada vez que se llama OnRingSafeCleared().")]
    [SerializeField] float fillRate = 0.15f;
    [Tooltip("Drenaje pasivo del medidor por segundo; obliga a mantener caida limpia sostenida.")]
    [SerializeField] float decayRate = 0.05f;
    [Tooltip("Penalizacion instantanea adicional al medidor cuando el jugador recibe un golpe.")]
    [SerializeField] float hitPenalty = 0.3f;
    [Tooltip("Duracion en segundos del estado Surge una vez activado.")]
    [SerializeField] float surgeDuration = 5f;
    [Tooltip("Multiplicador de score que ScoreManager debe aplicar mientras Surge esta activo.")]
    [SerializeField] int scoreMultiplier = 2;

    public float Meter     { get; private set; }
    public bool  IsSurging { get; private set; }
    public int   ScoreMultiplier => IsSurging ? scoreMultiplier : 1;

    float surgeTimeRemaining;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        GameManager.OnRunStart += HandleRunStart;
        GameManager.OnRunEnd   += HandleRunEnd;
    }

    void OnDisable()
    {
        GameManager.OnRunStart -= HandleRunStart;
        GameManager.OnRunEnd   -= HandleRunEnd;
    }

    void Update()
    {
        if (IsSurging)
        {
            surgeTimeRemaining -= Time.deltaTime;
            if (surgeTimeRemaining <= 0f) EndSurge();
            return;
        }

        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.State.Playing) return;

        SetMeter(Meter - decayRate * Time.deltaTime);
    }

    // ── API publica: otros sistemas llaman esto por referencia directa ──────
    public void OnRingSafeCleared()
    {
        if (IsSurging) return;
        SetMeter(Meter + fillRate);
        if (Meter >= 1f) StartSurge();
    }

    public void OnPlayerHit()
    {
        if (IsSurging) return;
        SetMeter(Meter - hitPenalty);
    }

    void SetMeter(float value)
    {
        Meter = Mathf.Clamp01(value);
        AbyssEvents.TriggerSurgeMeterChanged(Meter);
    }

    // Los [ContextMenu] permiten forzar el Surge a mano desde el Inspector
    // (clic derecho en el componente) para probar el VFX/feedback sin tener
    // que llenar el medidor jugando.
    [ContextMenu("Debug: Forzar Surge Start")]
    void StartSurge()
    {
        IsSurging = true;
        surgeTimeRemaining = surgeDuration;
        AbyssEvents.TriggerSurgeActivated();
    }

    [ContextMenu("Debug: Forzar Surge End")]
    void EndSurge()
    {
        IsSurging = false;
        SetMeter(0f);
        AbyssEvents.TriggerSurgeDeactivated();
    }

    void HandleRunStart() => ResetState();
    void HandleRunEnd(int depth, string cause, float durationSeconds) => ResetState();

    void ResetState()
    {
        IsSurging = false;
        surgeTimeRemaining = 0f;
        SetMeter(0f);
    }
}
