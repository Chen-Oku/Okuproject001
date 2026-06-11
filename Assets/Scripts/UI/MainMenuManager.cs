using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject scoresPanel;
    [SerializeField] GameObject howToPlayPanel;
    [SerializeField] GameObject settingsPanel;

    [Header("Scores Panel")]
    [FormerlySerializedAs("bestScoreText")]
    [SerializeField] TextMeshProUGUI leaderboardText;

    [Header("Settings Panel")]
    [SerializeField] Toggle musicToggle;
    [SerializeField] Toggle sfxToggle;

    [Header("Config")]
    [SerializeField] string gameSceneName = "Game";

    void Start()
    {
        ShowPanel(mainPanel);

        if (AudioManager.Instance == null) return;
        if (musicToggle != null) musicToggle.isOn = AudioManager.Instance.MusicEnabled;
        if (sfxToggle   != null) sfxToggle.isOn   = AudioManager.Instance.SfxEnabled;
    }

    // --- Navegación principal ---

    public void OnPlayButton()
    {
        PlayClick();
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnScoresButton()
    {
        PlayClick();
        RefreshLeaderboard();
        ShowPanel(scoresPanel);
    }

    public void OnClearScoresButton()
    {
        PlayClick();
        LeaderboardManager.Clear();
        PlayerPrefs.SetInt("BestScore", 0);
        PlayerPrefs.Save();
        RefreshLeaderboard();
    }

    public void OnHowToPlayButton()
    {
        PlayClick();
        ShowPanel(howToPlayPanel);
    }

    public void OnSettingsButton()
    {
        PlayClick();
        ShowPanel(settingsPanel);
    }

    public void OnBackButton()
    {
        PlayClick();
        ShowPanel(mainPanel);
    }

    public void OnQuitButton()
    {
        PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // --- Configuracion ---

    public void OnMusicToggle(bool value)
    {
        AudioManager.Instance?.SetMusic(value);
    }

    public void OnSfxToggle(bool value)
    {
        AudioManager.Instance?.SetSfx(value);
    }

    // --- Helpers ---

    void RefreshLeaderboard()
    {
        if (leaderboardText == null) return;

        var entries = LeaderboardManager.GetEntries();
        var sb = new StringBuilder();

        for (int i = 0; i < LeaderboardManager.MaxEntries; i++)
        {
            if (i < entries.Count)
                sb.AppendLine($"{i + 1,2}. {entries[i].Name,-10} {entries[i].Score}");
            else
                sb.AppendLine($"{i + 1,2}. {"---",-10} ---");
        }

        leaderboardText.text = sb.ToString();
    }

    void ShowPanel(GameObject target)
    {
        mainPanel.SetActive(mainPanel         == target);
        scoresPanel.SetActive(scoresPanel     == target);
        howToPlayPanel.SetActive(howToPlayPanel == target);
        settingsPanel.SetActive(settingsPanel == target);
    }

    void PlayClick()
    {
        AudioManager.Instance?.PlayButtonClick();
    }
}
