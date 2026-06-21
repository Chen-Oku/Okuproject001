using System.Collections;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI comboText;    // Asignar en Inspector; puede ser null
    [SerializeField] TextMeshProUGUI zoneText;    // Asignar en Inspector; puede ser null
    [SerializeField] TextMeshProUGUI shardsText;  // Asignar en Inspector; puede ser null
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TextMeshProUGUI finalScoreText;
    [SerializeField] TextMeshProUGUI bestScoreText;
    [SerializeField] GameObject pauseButton;

    [Header("Leaderboard - Game Over")]
    [SerializeField] GameObject nameEntryGroup;
    [SerializeField] TMP_InputField nameInputField;
    [SerializeField] GameObject saveScoreButton;
    [SerializeField] TextMeshProUGUI savedMessageText;

    Coroutine comboPulse;
    Coroutine zoneAnnounce;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        gameOverPanel.SetActive(false);
        scoreText.text = "0";
        if (comboText != null) comboText.gameObject.SetActive(false);
        if (zoneText  != null) zoneText.gameObject.SetActive(false);
        if (nameInputField != null) nameInputField.characterLimit = LeaderboardManager.MaxNameLength;
    }

    void OnEnable()  => CurrencyManager.OnShardsChanged += UpdateShards;
    void OnDisable() => CurrencyManager.OnShardsChanged -= UpdateShards;

    void Start()
    {
        UpdateShards(CurrencyManager.Instance != null ? CurrencyManager.Instance.Get() : 0);
    }

    public void UpdateScore(int score)
    {
        scoreText.text = score.ToString();
    }

    void UpdateShards(int amount)
    {
        if (shardsText == null) return;
        shardsText.text = amount.ToString();
    }

    public void UpdateCombo(int streak)
    {
        if (comboText == null) return;

        if (streak < 2)
        {
            comboText.gameObject.SetActive(false);
            return;
        }

        comboText.gameObject.SetActive(true);
        comboText.text = $"x{streak} COMBO!";

        if (comboPulse != null) StopCoroutine(comboPulse);
        comboPulse = StartCoroutine(PulseCombo());
    }

    IEnumerator PulseCombo()
    {
        comboText.transform.localScale = Vector3.one * 1.4f;
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1.4f, 1f, t / 0.2f);
            comboText.transform.localScale = Vector3.one * s;
            yield return null;
        }
        comboText.transform.localScale = Vector3.one;
    }

    public void ShowZoneAnnouncement(string zoneName)
    {
        if (zoneText == null) return;
        if (zoneAnnounce != null) StopCoroutine(zoneAnnounce);
        zoneAnnounce = StartCoroutine(AnnounceZone(zoneName));
    }

    IEnumerator AnnounceZone(string zoneName)
    {
        zoneText.gameObject.SetActive(true);
        zoneText.text = zoneName;

        Color c = zoneText.color;
        c.a = 1f;
        zoneText.color = c;

        yield return new WaitForSeconds(1.5f);

        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / 0.6f);
            zoneText.color = c;
            yield return null;
        }

        zoneText.gameObject.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (comboText != null) comboText.gameObject.SetActive(false);
        if (zoneText  != null) zoneText.gameObject.SetActive(false);
        int score = ScoreManager.Instance.Score;
        finalScoreText.text = score.ToString();
        bestScoreText.text = $"Best Score: {ScoreManager.Instance.BestScore}";
        gameOverPanel.SetActive(true);
        if (pauseButton != null) pauseButton.SetActive(false);

        if (nameEntryGroup != null)
        {
            bool qualifies = LeaderboardManager.QualifiesForLeaderboard(score);
            nameEntryGroup.SetActive(qualifies);
            if (qualifies)
            {
                if (nameInputField != null)
                {
                    nameInputField.gameObject.SetActive(true);
                    nameInputField.text = "";
                }
                if (saveScoreButton != null) saveScoreButton.SetActive(true);
                if (savedMessageText != null) savedMessageText.gameObject.SetActive(false);
            }
        }
    }

    // Llamado desde el boton "Guardar" del campo de nombre en GameOver
    public void OnSaveScoreButton()
    {
        if (nameInputField == null) return;
        LeaderboardManager.AddScore(nameInputField.text, ScoreManager.Instance.Score);

        nameInputField.gameObject.SetActive(false);
        if (saveScoreButton != null) saveScoreButton.SetActive(false);
        if (savedMessageText != null)
        {
            savedMessageText.text = "Saved Score!";
            savedMessageText.gameObject.SetActive(true);
        }
    }

    // Llamado desde el boton "Reintentar" en el panel GameOver
    public void OnRestartButton()
    {
        GameManager.Instance.Restart();
    }

    // Llamado desde el boton "Menu" en el panel GameOver
    public void OnMainMenuButton()
    {
        GameManager.Instance.GoToMainMenu();
    }
}
