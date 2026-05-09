/* ===============================
 * Written by salma
 * Last Modified: 09 / 05 / 2026
 * ===============================
*/

using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    private const string BGM_KEY = "bgmVolume";
    private const string SFX_KEY = "sfxVolume";

    private float currentBGM = 0.8f;
    private float currentSFX = 0.8f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumes();
    }

    // -----------------------------
    // MUSIC
    // -----------------------------
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopMusic()
    {
        bgmSource.Stop();
    }

    // -----------------------------
    // SFX
    // -----------------------------
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        sfxSource.PlayOneShot(clip);
    }

    // -----------------------------
    // VOLUME
    // -----------------------------
    public void SetBGMVolume(float value)
    {
        currentBGM = value;

        audioMixer.SetFloat(
            "BGMVolume",
            Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20
        );

        PlayerPrefs.SetFloat(BGM_KEY, value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        currentSFX = value;

        audioMixer.SetFloat(
            "SFXVolume",
            Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20
        );

        PlayerPrefs.SetFloat(SFX_KEY, value);
        PlayerPrefs.Save();
    }

    public float GetBGMVolume() => currentBGM;
    public float GetSFXVolume() => currentSFX;

    private void LoadVolumes()
    {
        currentBGM = PlayerPrefs.GetFloat(BGM_KEY, 0.8f);
        currentSFX = PlayerPrefs.GetFloat(SFX_KEY, 0.8f);

        SetBGMVolume(currentBGM);
        SetSFXVolume(currentSFX);
    }
}