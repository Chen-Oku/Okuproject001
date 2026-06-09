using UnityEngine;
using UnityEngine.SceneManagement;
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
    [SerializeField] TextMeshProUGUI bestScoreText;

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
        int best = PlayerPrefs.GetInt("BestScore", 0);
        if (bestScoreText != null)
            bestScoreText.text = best > 0 ? best.ToString() : "---";
        ShowPanel(scoresPanel);
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
