using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int Score { get; private set; }
    public int BestScore { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BestScore = PlayerPrefs.GetInt("BestScore", 0);
    }

    public void AddScore(int amount = 1)
    {
        Score += amount;
        UIManager.Instance.UpdateScore(Score);
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
