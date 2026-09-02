/* ===============================
 * Written by salma
 * Last Modified: 09 / 05 / 2026
 * ===============================
*/

using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.DebugUI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sound Clips")]
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip scoreProgressSound;
    [SerializeField] private AudioClip guessSound;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    private const string BGM_KEY = "bgmVolume";
    private const string SFX_KEY = "sfxVolume";

    private const string BGM_PARAM = "BGMVolume";
    private const string SFX_PARAM = "SFXVolume";

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

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        LoadVolumes();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSound);
    }

    public void PlayGuessSFX()
    {
        PlaySFX(guessSound);
    }

    // -----------------------------
    // MUSIC
    // -----------------------------
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        // Prevent restarting same track
        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

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
    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        sfxSource.PlayOneShot(clip);
    }

    private Coroutine scoreFadeRoutine;

    public void PlayScoreSFX()
    {
        if (scoreProgressSound == null) return;

        if (scoreFadeRoutine != null)
        {
            StopCoroutine(scoreFadeRoutine);
            scoreFadeRoutine = null;
        }

        sfxSource.clip = scoreProgressSound;
        sfxSource.loop = true;
        sfxSource.volume = 1f;
        sfxSource.Play();
    }

    public void StopScoreSFX(float fadeDuration = 0.3f)
    {
        if (scoreFadeRoutine != null)
            StopCoroutine(scoreFadeRoutine);

        scoreFadeRoutine = StartCoroutine(FadeOutScoreSFX(fadeDuration));
    }

    private IEnumerator FadeOutScoreSFX(float duration)
    {
        float startVolume = sfxSource.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            sfxSource.volume = Mathf.Lerp(startVolume, 0f, time / duration);
            yield return null;
        }

        sfxSource.Stop();
        sfxSource.loop = false;
        sfxSource.volume = 1f; // reset for next play
        scoreFadeRoutine = null;
    }

    // -----------------------------
    // VOLUME
    // -----------------------------
    public void SetBGMVolume(float value)
    {
        currentBGM = value;

        audioMixer.SetFloat(
            BGM_PARAM,
            Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20
        );

        PlayerPrefs.SetFloat(BGM_KEY, value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        currentSFX = value;

        audioMixer.SetFloat(
            SFX_PARAM,
            Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20
        );

        PlayerPrefs.SetFloat(SFX_KEY, value);
        PlayerPrefs.Save();
    }

    public float GetBGMVolume() => currentBGM;
    public float GetSFXVolume() => currentSFX;

    private void LoadVolumes()
    {
        currentBGM =
            PlayerPrefs.GetFloat(BGM_KEY, 0.8f);

        currentSFX =
            PlayerPrefs.GetFloat(SFX_KEY, 0.8f);

        audioMixer.SetFloat(
             BGM_PARAM,
             Mathf.Log10(Mathf.Max(currentBGM, 0.0001f)) * 20
         );

        audioMixer.SetFloat(
            SFX_PARAM,
            Mathf.Log10(Mathf.Max(currentSFX, 0.0001f)) * 20
        );

        Debug.Log($"Loaded audio settings: BGM={currentBGM}, SFX={currentSFX}");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode arg1)
    {
        string sceneName = scene.name;

        // STOP music in map selection
        if (scene.name == SceneNames.MAP_SELECTION)
        {
            StopMusic();
            return;
        }

        PlayMusic(gameplayMusic);
    }

}