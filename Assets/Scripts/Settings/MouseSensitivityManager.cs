using UnityEngine;
using UnityEngine.UI;

/***
 * 
 * MouseSensitivityManager, responsible for adjusting mouse sensitivity for camera controls
 * 
 * Written by aleu0007
 * Last Modified: 13/08/2025
 * 
 */
public class MouseSensitivityManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider sensitivitySlider;
    
    [Header("Sensitivity Settings")]
    [SerializeField] private float minSensitivity = 0.1f;
    [SerializeField] private float maxSensitivity = 10.0f;
    [SerializeField] private float defaultSensitivity = 5.0f;
    
    [Header("Camera References")]
    [SerializeField] private RotateCamera[] rotateCameras;
    
    private const string SENSITIVITY_PREF_KEY = "mouseSensitivity";
    
    void Awake()
    {
        // Auto-assign slider if not set
        if (sensitivitySlider == null)
        {
            sensitivitySlider = GetComponent<Slider>();
            if (sensitivitySlider == null)
            {
                sensitivitySlider = GetComponentInChildren<Slider>();
            }
        }
        
        // Set up slider properties
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = minSensitivity;
            sensitivitySlider.maxValue = maxSensitivity;
            
            // Automatically wire up the OnValueChanged event
            sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
        else
        {
            Debug.LogError("No Slider found! Please assign a slider to the MouseSensitivityManager or attach this script to a GameObject with a Slider component.", this);
        }
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Auto-find RotateCamera scripts if none are assigned
        if (rotateCameras == null || rotateCameras.Length == 0)
        {
            AutoFindRotateCameras();
        }
        
        // Load saved sensitivity or use default
        LoadSensitivity();
    }
    
    /// <summary>
    /// Automatically finds all RotateCamera scripts in the scene
    /// </summary>
    private void AutoFindRotateCameras()
    {
        rotateCameras = FindObjectsByType<RotateCamera>(FindObjectsSortMode.None);
        
        if (rotateCameras.Length == 0)
        {
            Debug.LogWarning("No RotateCamera scripts found in the scene. Please assign them manually or ensure RotateCamera scripts are present.", this);
        }
        else
        {
            Debug.Log($"Found {rotateCameras.Length} RotateCamera script(s) in the scene.", this);
        }
    }
    
    /// <summary>
    /// Called when the slider value changes
    /// </summary>
    private void OnSensitivityChanged(float value)
    {
        SetMouseSensitivity(value);
        SaveSensitivity();
        
        Debug.Log($"Mouse sensitivity set to: {value:F2}");
    }
    
    /// <summary>
    /// Sets the mouse sensitivity for all RotateCamera scripts
    /// </summary>
    public void SetMouseSensitivity(float sensitivity)
    {
        foreach (RotateCamera rotateCamera in rotateCameras)
        {
            if (rotateCamera != null)
            {
                rotateCamera.mouseSensitivity = sensitivity;
            }
        }
    }
    
    /// <summary>
    /// Saves the current sensitivity to PlayerPrefs
    /// </summary>
    public void SaveSensitivity()
    {
        if (sensitivitySlider != null)
        {
            PlayerPrefs.SetFloat(SENSITIVITY_PREF_KEY, sensitivitySlider.value);
            PlayerPrefs.Save();
        }
    }
    
    /// <summary>
    /// Loads the saved sensitivity from PlayerPrefs
    /// </summary>
    public void LoadSensitivity()
    {
        float savedSensitivity = PlayerPrefs.GetFloat(SENSITIVITY_PREF_KEY, defaultSensitivity);
        
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = savedSensitivity;
        }
        
        // Apply the loaded sensitivity immediately
        SetMouseSensitivity(savedSensitivity);
    }
    
    /// <summary>
    /// Resets sensitivity to default value
    /// </summary>
    public void ResetToDefault()
    {
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = defaultSensitivity;
        }
        SetMouseSensitivity(defaultSensitivity);
        SaveSensitivity();
    }
    
    /// <summary>
    /// Adds a RotateCamera script to be controlled by this manager
    /// </summary>
    public void AddRotateCamera(RotateCamera rotateCamera)
    {
        if (rotateCamera != null)
        {
            // Resize array and add new camera
            System.Array.Resize(ref rotateCameras, rotateCameras.Length + 1);
            rotateCameras[rotateCameras.Length - 1] = rotateCamera;
            
            // Apply current sensitivity to the new camera
            if (sensitivitySlider != null)
            {
                rotateCamera.mouseSensitivity = sensitivitySlider.value;
            }
        }
    }
    
    /// <summary>
    /// Gets the current sensitivity value
    /// </summary>
    public float GetCurrentSensitivity()
    {
        return sensitivitySlider != null ? sensitivitySlider.value : defaultSensitivity;
    }
}
