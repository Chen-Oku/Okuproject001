using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum State { Playing, GameOver }
    public State CurrentState { get; private set; } = State.Playing;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void TriggerGameOver()
    {
        if (CurrentState == State.GameOver) return;
        CurrentState = State.GameOver;
        ScoreManager.Instance.SaveBestScore();
        UIManager.Instance.ShowGameOver();
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
