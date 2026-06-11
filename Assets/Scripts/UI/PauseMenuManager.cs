using UnityEngine;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] GameObject pausePanel;
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject optionsPanel;

    [Header("Options Panel")]
    [SerializeField] Toggle musicToggle;
    [SerializeField] Toggle sfxToggle;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        pausePanel.SetActive(false);
    }

    public void Show()
    {
        ShowPanel(mainPanel);
        pausePanel.SetActive(true);

        if (AudioManager.Instance == null) return;
        if (musicToggle != null) musicToggle.isOn = AudioManager.Instance.MusicEnabled;
        if (sfxToggle   != null) sfxToggle.isOn   = AudioManager.Instance.SfxEnabled;
    }

    public void Hide()
    {
        pausePanel.SetActive(false);
    }

    // --- Panel principal ---

    public void OnResumeButton()
    {
        PlayClick();
        GameManager.Instance.Resume();
    }

    public void OnRestartButton()
    {
        PlayClick();
        GameManager.Instance.Restart();
    }

    public void OnOptionsButton()
    {
        PlayClick();
        ShowPanel(optionsPanel);
    }

    public void OnMainMenuButton()
    {
        PlayClick();
        GameManager.Instance.GoToMainMenu();
    }

    public void OnExitButton()
    {
        PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // --- Panel de opciones ---

    public void OnBackButton()
    {
        PlayClick();
        ShowPanel(mainPanel);
    }

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
        mainPanel.SetActive(mainPanel == target);
        optionsPanel.SetActive(optionsPanel == target);
    }

    void PlayClick()
    {
        AudioManager.Instance?.PlayButtonClick();
    }
}
