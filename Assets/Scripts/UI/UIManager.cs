using System.Collections;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI comboText;    // Asignar en Inspector; puede ser null
    [SerializeField] TextMeshProUGUI zoneText;    // Asignar en Inspector; puede ser null
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TextMeshProUGUI finalScoreText;
    [SerializeField] TextMeshProUGUI bestScoreText;

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
    }

    public void UpdateScore(int score)
    {
        scoreText.text = score.ToString();
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
        finalScoreText.text = ScoreManager.Instance.Score.ToString();
        bestScoreText.text = $"MEJOR: {ScoreManager.Instance.BestScore}";
        gameOverPanel.SetActive(true);
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
