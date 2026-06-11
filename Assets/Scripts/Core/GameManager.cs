using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum State { Playing, Paused, GameOver }
    public State CurrentState { get; private set; } = State.Playing;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Time.timeScale = 1f;
    }

    public void TriggerGameOver()
    {
        if (CurrentState == State.GameOver) return;
        CurrentState = State.GameOver;
        ScoreManager.Instance.SaveBestScore();
        UIManager.Instance.ShowGameOver();
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
