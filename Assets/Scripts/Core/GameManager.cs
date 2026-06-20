using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum State { WaitingToStart, Playing, Paused, GameOver }
    public State CurrentState { get; private set; } = State.WaitingToStart;

    // Analytics: publicados para que sistemas nuevos (AnalyticsManager) escuchen sin acoplar este manager a ellos.
    public static event Action OnRunStart;
    public static event Action<int /*depth*/, string /*cause*/, float /*durationSeconds*/> OnRunEnd;

    float runStartTime;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Time.timeScale = 1f;
    }

    public void StartGame()
    {
        if (CurrentState != State.WaitingToStart) return;
        CurrentState = State.Playing;
        runStartTime = Time.time;
        OnRunStart?.Invoke();
    }

    public void TriggerGameOver(string cause = "Unknown")
    {
        if (CurrentState == State.GameOver) return;
        CurrentState = State.GameOver;
        ScoreManager.Instance.SaveBestScore();
        UIManager.Instance.ShowGameOver();

        int depth = ZoneManager.Instance != null ? ZoneManager.Instance.RingsPassed : 0;
        OnRunEnd?.Invoke(depth, cause, Time.time - runStartTime);
    }

    public void Pause()
    {
        if (CurrentState != State.Playing) return;
        CurrentState = State.Paused;
        Time.timeScale = 0f;
        PauseMenuManager.Instance.Show();
    }

    public void Resume()
    {
        if (CurrentState != State.Paused) return;
        CurrentState = State.Playing;
        Time.timeScale = 1f;
        PauseMenuManager.Instance.Hide();
    }

    public void TogglePause()
    {
        if (CurrentState == State.Playing) Pause();
        else if (CurrentState == State.Paused) Resume();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
