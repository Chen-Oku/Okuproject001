using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int Score      { get; private set; }
    public int BestScore  { get; private set; }
    public int ComboStreak { get; private set; }
    public int ComboMax    { get; private set; }

    // Analytics: se dispara cada vez que el combo de la run supera su maximo previo.
    public static event Action<int /*newMax*/> OnNewComboMax;

    // Feedback continuo (FOV/shake): se dispara en cada llamada, incluido el reset a 0.
    public static event Action<int /*currentStreak*/> OnComboChanged;

    public const int MaxCombo = 10;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BestScore = PlayerPrefs.GetInt("BestScore", 0);
    }

    void OnEnable()  => NearMissSystem.OnNearMiss += HandleNearMiss;
    void OnDisable() => NearMissSystem.OnNearMiss -= HandleNearMiss;

    // El near-miss alimenta el mismo combo que el skip de anillo: un solo contador.
    void HandleNearMiss(RingSegment segment) => AddScoreWithCombo(skipped: true);

    // Mantener compatibilidad con cualquier llamada directa existente
    public void AddScore(int amount = 1)
    {
        Score += amount;
        UIManager.Instance.UpdateScore(Score);
    }

    public void AddScoreWithCombo(bool skipped)
    {
        if (skipped)
            ComboStreak = Mathf.Min(ComboStreak + 1, MaxCombo);
        else
            ComboStreak = 0;

        int points = Mathf.Max(1, ComboStreak);
        Score += points;

        if (ComboStreak > ComboMax)
        {
            ComboMax = ComboStreak;
            OnNewComboMax?.Invoke(ComboMax);
        }

        UIManager.Instance.UpdateScore(Score);
        UIManager.Instance.UpdateCombo(ComboStreak);
        OnComboChanged?.Invoke(ComboStreak);
    }

    public void SaveBestScore()
    {
        if (Score > BestScore)
        {
            BestScore = Score;
            PlayerPrefs.SetInt("BestScore", BestScore);
            PlayerPrefs.Save();
        }
    }
}
