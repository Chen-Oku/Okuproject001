using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioClip buttonClickClip;

    const string MusicKey = "MusicEnabled";
    const string SfxKey   = "SfxEnabled";

    public bool MusicEnabled { get; private set; }
    public bool SfxEnabled   { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        MusicEnabled = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        SfxEnabled   = PlayerPrefs.GetInt(SfxKey,   1) == 1;
        ApplyMusic();
    }

    public void PlayButtonClick()
    {
        if (!SfxEnabled || buttonClickClip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(buttonClickClip);
    }

    public void SetMusic(bool enabled)
    {
        MusicEnabled = enabled;
        PlayerPrefs.SetInt(MusicKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusic();
    }

    public void SetSfx(bool enabled)
    {
        SfxEnabled = enabled;
        PlayerPrefs.SetInt(SfxKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    void ApplyMusic()
    {
        if (musicSource == null) return;
        if (MusicEnabled) { if (!musicSource.isPlaying) musicSource.Play(); }
        else musicSource.Stop();
    }
}
