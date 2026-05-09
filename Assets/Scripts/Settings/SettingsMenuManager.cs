using UnityEngine;
using UnityEngine.UI;

/***
 * 
 * SettingsMenuManager, responsible for showing/hiding the settings UI overlay
 * 
 * Written by aleu0007
 * Last Modified: 13/08/2025
 * 
 */
public class SettingsMenuManager : MonoBehaviour
{
    [Header("Settings Configuration")]
    [SerializeField] private GameObject settingsComponent;
    [SerializeField] private Transform canvasParent;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button closeButton;
    
    [Header("Overlay Settings")]
    [SerializeField] private bool pauseGameWhenOpen = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;
    
    private GameObject settingsInstance;
    private bool isSettingsOpen = false;
    
    void Start()
    {
        // Auto-find canvas if not assigned
        if (canvasParent == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                canvasParent = canvas.transform;
            }
            else
            {
                Debug.LogError("No Canvas found! Please assign a Canvas as the parent for the settings overlay.", this);
            }
        }
        
        // Auto-wire button if attached to the same GameObject
        if (settingsButton == null)
        {
            settingsButton = GetComponent<Button>();
        }
        
        // Wire up the button click event
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(ToggleSettings);
        }
        else
        {
            Debug.LogWarning("No Settings Button assigned. You can still use ToggleSettings() method or the toggle key.", this);
        }
        
        // Validate prefab assignment
        if (settingsComponent == null)
        {
            Debug.LogError("Settings Prefab is not assigned! Please assign your settings prefab in the inspector.", this);
        }
    }
    
    void Update()
    {
        // Allow keyboard toggle
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleSettings();
        }
    }
    
    /// <summary>
    /// Toggles the settings menu open/closed
    /// </summary>
    public void ToggleSettings()
    {
        if (isSettingsOpen)
        {
            CloseSettings();
        }
        else
        {
            OpenSettings();
        }
    }
    
    /// <summary>
    /// Opens the settings menu
    /// </summary>
    public void OpenSettings()
    {
        if (settingsComponent == null || canvasParent == null)
        {
            Debug.LogError("Cannot open settings: Missing prefab or canvas parent reference.", this);
            return;
        }
        
        if (settingsInstance == null)
        {
            // Instantiate the settings prefab
            settingsInstance = Instantiate(settingsComponent, canvasParent);
            
            // Find and wire up close button in the instantiated prefab
            FindAndWireCloseButton();
        }
        else
        {
            // Just activate existing instance
            settingsInstance.SetActive(true);
        }
        
        isSettingsOpen = true;
        
        // Pause game if enabled
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 0f;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalCall("hideMapFromUnity");
#endif
        
        Debug.Log("Settings menu opened.");
    }
    
    /// <summary>
    /// Closes the settings menu
    /// </summary>
    public void CloseSettings()
    {
        if (settingsInstance != null)
        {
            settingsInstance.SetActive(false);
        }
        
        isSettingsOpen = false;
        
        // Resume game if it was paused
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalCall("showMapFromUnity");
#endif
        
        Debug.Log("Settings menu closed.");
    }
    
    /// <summary>
    /// Destroys the settings instance completely (useful for cleanup)
    /// </summary>
    public void DestroySettings()
    {
        if (settingsInstance != null)
        {
            Destroy(settingsInstance);
            settingsInstance = null;
        }
        
        isSettingsOpen = false;
        
        // Resume game if it was paused
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
        }
    }
    
    /// <summary>
    /// Finds and wires up close button in the settings prefab
    /// </summary>
    private void FindAndWireCloseButton()
    {
        if (settingsInstance == null) return;
        
        // Try to find close button by name
        Transform closeButtonTransform = settingsInstance.transform.Find("CloseButton");
        if (closeButtonTransform == null)
        {
            // Try alternative names
            closeButtonTransform = settingsInstance.transform.Find("Close");
            if (closeButtonTransform == null)
            {
                closeButtonTransform = settingsInstance.transform.Find("X");
            }
        }
        
        // If still not found, search in children
        if (closeButtonTransform == null)
        {
            Button[] buttons = settingsInstance.GetComponentsInChildren<Button>();
            foreach (Button btn in buttons)
            {
                if (btn.name.ToLower().Contains("close") || btn.name.ToLower().Contains("x"))
                {
                    closeButtonTransform = btn.transform;
                    break;
                }
            }
        }
        
        // Wire up the close button
        if (closeButtonTransform != null)
        {
            Button closeBtn = closeButtonTransform.GetComponent<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.AddListener(CloseSettings);
                Debug.Log($"Close button '{closeBtn.name}' found and wired up automatically.");
            }
        }
        else
        {
            Debug.LogWarning("No close button found in settings prefab. You may need to manually call CloseSettings() or assign the close button reference.");
        }
    }
    
    /// <summary>
    /// Manually set the close button reference (call this if auto-detection fails)
    /// </summary>
    public void SetCloseButton(Button closeBtn)
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseSettings);
        }
        
        closeButton = closeBtn;
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseSettings);
        }
    }
    
    /// <summary>
    /// Check if settings menu is currently open
    /// </summary>
    public bool IsSettingsOpen()
    {
        return isSettingsOpen;
    }
    
    void OnDestroy()
    {
        // Clean up button listeners
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(ToggleSettings);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseSettings);
        }
        
        // Resume game time in case this object is destroyed while settings are open
        if (pauseGameWhenOpen && isSettingsOpen)
        {
            Time.timeScale = 1f;
        }
    }
}
