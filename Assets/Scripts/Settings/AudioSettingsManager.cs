/* ===============================
 * Written by salma
 * Last Modified: 09 / 05 / 2026
 * ===============================
*/
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Defaults")]
    [SerializeField] private float defaultBGM = 0.8f;
    [SerializeField] private float defaultSFX = 0.8f;

    private void Awake()
    {
        SetupSlider(bgmSlider, OnBGMChanged);
        SetupSlider(sfxSlider, OnSFXChanged);
    }

    private void Start()
    {
        LoadAudioSettings();
    }

    private void SetupSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null) return;

        slider.minValue = 0.0001f;
        slider.maxValue = 1f;

        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(callback);
    }

    private void LoadAudioSettings()
    {
        if (bgmSlider != null)
        {
            bgmSlider.value = AudioManager.Instance != null
                ? AudioManager.Instance.GetBGMVolume()
                : defaultBGM;
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = AudioManager.Instance != null
                ? AudioManager.Instance.GetSFXVolume()
                : defaultSFX;
        }
    }

    private void OnBGMChanged(float value)
    {
        AudioManager.Instance?.SetBGMVolume(value);
    }

    private void OnSFXChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
    }

    public void ResetToDefaults()
    {
        if (bgmSlider != null)
            bgmSlider.value = defaultBGM;

        if (sfxSlider != null)
            sfxSlider.value = defaultSFX;
    }
}