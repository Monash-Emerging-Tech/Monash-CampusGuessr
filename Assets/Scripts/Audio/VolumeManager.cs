using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/***
 * 
 * VolumeManager, responsible for adjusting the volume level of different audio sources
 * 
 * Written by aleu0007
 * Last Modified: 13/08/2025
 * 
 */
public class VolumeManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider volumeSlider;
    
    [Header("Audio Configuration")]
    [SerializeField] private VolumeType volumeType = VolumeType.Master;
    [SerializeField] private List<AudioSource> audioSources = new List<AudioSource>();
    
    // Volume types for different audio categories
    public enum VolumeType
    {
        Master,     // Affects AudioListener.volume (all audio)
        Music,      // Background music
        SFX,        // Sound effects
        UI,         // UI sounds
        Custom      // Custom audio sources
    }
    
    private string prefsKey;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Set the preferences key based on volume type
        prefsKey = volumeType.ToString().ToLower() + "Volume";
        
        // Auto-find audio sources if none are assigned and not using Master volume
        if (audioSources.Count == 0 && volumeType != VolumeType.Master)
        {
            AutoFindAudioSources();
        }
        
        if (PlayerPrefs.HasKey(prefsKey))
            LoadVolume();
        else
        {
            PlayerPrefs.SetFloat(prefsKey, 1);
            LoadVolume();
        }
    }
    
    /// <summary>
    /// Automatically finds audio sources based on volume type
    /// </summary>
    private void AutoFindAudioSources()
    {
        switch (volumeType)
        {
            case VolumeType.Music:
                FindAudioSourcesByTag("Music");
                break;
            case VolumeType.SFX:
                FindAudioSourcesByTag("SFX");
                break;
            case VolumeType.UI:
                FindAudioSourcesByTag("UI");
                break;
            case VolumeType.Custom: // manually assign audio sources
                Debug.LogWarning("Custom volume type selected but no audio sources assigned. Please assign them manually.", this);
                break;
        }
    }
    
    /// <summary>
    /// Finds audio sources by tag and adds them to the list
    /// </summary>
    private void FindAudioSourcesByTag(string tag)
    {
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in taggedObjects)
        {
            AudioSource audioSource = obj.GetComponent<AudioSource>();
            if (audioSource != null && !audioSources.Contains(audioSource))
            {
                audioSources.Add(audioSource);
            }
        }
        
        if (audioSources.Count == 0)
        {
            Debug.LogWarning($"No AudioSources found with tag '{tag}'. You may need to assign them manually or tag your audio GameObjects.", this);
        }
    }

    public void SetVolume()
    {
        float volume = volumeSlider.value;
        
        switch (volumeType)
        {
            case VolumeType.Master:
                // Affects all audio in the scene
                AudioListener.volume = volume;
                break;
                
            default:
                // Affects specific audio sources
                foreach (AudioSource audioSource in audioSources)
                {
                    if (audioSource != null)
                    {
                        audioSource.volume = volume;
                    }
                }
                break;
        }
        
        SaveVolume();
        Debug.Log($"[VolumeManager] {volumeType} volume set to: {volume:F2}");
    }

    public void SaveVolume()
    {
        PlayerPrefs.SetFloat(prefsKey, volumeSlider.value);
    }

    public void LoadVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat(prefsKey);
        volumeSlider.value = savedVolume;
        
        // Apply the loaded volume immediately
        SetVolumeWithoutSaving(savedVolume);
    }
    
    /// <summary>
    /// Sets the volume without saving to PlayerPrefs (used during loading)
    /// </summary>
    private void SetVolumeWithoutSaving(float volume)
    {
        switch (volumeType)
        {
            case VolumeType.Master:
                AudioListener.volume = volume;
                break;
                
            default:
                foreach (AudioSource audioSource in audioSources)
                {
                    if (audioSource != null)
                    {
                        audioSource.volume = volume;
                    }
                }
                break;
        }
    }
    
    /// <summary>
    /// Adds an audio source to the list manually
    /// </summary>
    public void AddAudioSource(AudioSource audioSource)
    {
        if (audioSource != null && !audioSources.Contains(audioSource))
        {
            audioSources.Add(audioSource);
            // Apply current volume to the new audio source
            if (volumeSlider != null)
            {
                audioSource.volume = volumeSlider.value;
            }
        }
    }
    
    /// <summary>
    /// Removes an audio source from the list
    /// </summary>
    public void RemoveAudioSource(AudioSource audioSource)
    {
        if (audioSources.Contains(audioSource))
        {
            audioSources.Remove(audioSource);
        }
    }
    
    /// <summary>
    /// Clears all audio sources from the list
    /// </summary>
    public void ClearAudioSources()
    {
        audioSources.Clear();
    }
}
