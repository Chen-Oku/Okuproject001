using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TextMeshProUGUI finalScoreText;
    [SerializeField] TextMeshProUGUI bestScoreText;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        gameOverPanel.SetActive(false);
        scoreText.text = "0";
    }

    public void UpdateScore(int score)
    {
        scoreText.text = score.ToString();
    }

    public void ShowGameOver()
    {
        finalScoreText.text = ScoreManager.Instance.Score.ToString();
        bestScoreText.text = $"MEJOR: {ScoreManager.Instance.BestScore}";
        gameOverPanel.SetActive(true);
    }

    // Llamado desde el boton "Reintentar" en el panel GameOver
    public void OnRestartButton()
    {
        GameManager.Instance.Restart();
    }
}
