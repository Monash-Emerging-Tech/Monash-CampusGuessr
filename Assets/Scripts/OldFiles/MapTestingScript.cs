using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Temporary testing script for simulating map interactions in Unity Editor
/// This script helps test the MapInteractionManager integration without needing WebGL build
/// 
/// Written by Assistant
/// Last Modified: 24/09/2025
/// </summary>
public class MapTestingScript : MonoBehaviour
{
    [Header("Test Coordinates")]
    [SerializeField] private float testLatitude = -37.8136f;  // Melbourne coordinates
    [SerializeField] private float testLongitude = 144.9631f;
    [SerializeField] private float testLatitude2 = -37.8200f; // Alternative test location
    [SerializeField] private float testLongitude2 = 144.9700f;
    
    [Header("Test Buttons (Optional)")]
    [SerializeField] private Button simulateGuessSubmitButton;
    [SerializeField] private Button simulateSecondLocationButton;
    
    [Header("Debug Display")]
    [SerializeField] private TextMeshProUGUI debugText;
    
    [Header("Keyboard Shortcuts")]
    [SerializeField] private KeyCode guessSubmitKey = KeyCode.G;
    [SerializeField] private KeyCode secondLocationKey = KeyCode.N;
    [SerializeField] private KeyCode resetRoundKey = KeyCode.R;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private bool useFirstLocation = true;
    
    void Start()
    {
        // Setup button listeners if buttons are assigned
        if (simulateGuessSubmitButton != null)
            simulateGuessSubmitButton.onClick.AddListener(SimulateGuessSubmit);
            
        if (simulateSecondLocationButton != null)
            simulateSecondLocationButton.onClick.AddListener(ToggleTestLocation);
        
        LogDebug("MapTestingScript initialized");
        LogDebug("Keyboard shortcuts:");
        LogDebug($"  {guessSubmitKey} - Simulate guess submit");
        LogDebug($"  {secondLocationKey} - Toggle between test locations");
        LogDebug($"  {resetRoundKey} - Reset round (if GameLogic available)");
        
        UpdateDebugDisplay();
    }
    
    void Update()
    {
        // Keyboard shortcuts for testing
        if (Input.GetKeyDown(guessSubmitKey))
        {
            SimulateGuessSubmit();
        }
        
        if (Input.GetKeyDown(secondLocationKey))
        {
            useFirstLocation = !useFirstLocation;
            LogDebug($"Switched to {(useFirstLocation ? "first" : "second")} test location");
            UpdateDebugDisplay();
        }
        
        if (Input.GetKeyDown(resetRoundKey))
        {
            ResetRound();
        }
    }
    
    /// <summary>
    /// Simulates guess submission with current test coordinates
    /// </summary>
    public void SimulateGuessSubmit()
    {
        if (MapInteractionManager.Instance == null)
        {
            LogError("MapInteractionManager.Instance is null! Make sure MapInteractionManager is assigned to a GameObject.");
            return;
        }
        
        float lat = useFirstLocation ? testLatitude : testLatitude2;
        float lng = useFirstLocation ? testLongitude : testLongitude2;
        
        string jsonData = $"{{\"latitude\": {lat}, \"longitude\": {lng}, \"timestamp\": {System.DateTimeOffset.Now.ToUnixTimeSeconds()}}}";
        
        MapInteractionManager.Instance.SubmitGuess(jsonData);
        LogDebug($"Simulated guess submit at: {lat}, {lng}");
        UpdateDebugDisplay();
    }
    
    /// <summary>
    /// Resets the current round (if GameLogic is available)
    /// </summary>
    public void ResetRound()
    {
        if (GameLogic.Instance != null)
        {
            GameLogic.Instance.nextRound();
            LogDebug("Reset round via GameLogic");
        }
        else
        {
            LogDebug("GameLogic.Instance is null - cannot reset round");
        }
        UpdateDebugDisplay();
    }
    
    /// <summary>
    /// Updates the debug display text
    /// </summary>
    private void UpdateDebugDisplay()
    {
        if (debugText != null)
        {
            string location = useFirstLocation ? $"Location 1: {testLatitude:F4}, {testLongitude:F4}" : 
                                              $"Location 2: {testLatitude2:F4}, {testLongitude2:F4}";
            
            string gameState = "";
            if (GameLogic.Instance != null)
            {
                gameState = $"\nGame State:\n" +
                           $"  Round: {GameLogic.Instance.GetCurrentRound()}\n" +
                           $"  Is Guessing: {GameLogic.Instance.IsGuessing()}\n" +
                           $"  Round Active: {GameLogic.Instance.IsRoundActive()}";
            }
            
            debugText.text = $"Map Testing Controls\n\n" +
                           $"Current: {location}\n" +
                           $"Shortcuts: {guessSubmitKey}, {secondLocationKey}, {resetRoundKey}" +
                           gameState;
        }
    }
    
    /// <summary>
    /// Sets test coordinates programmatically
    /// </summary>
    public void SetTestCoordinates(float latitude, float longitude)
    {
        if (useFirstLocation)
        {
            testLatitude = latitude;
            testLongitude = longitude;
        }
        else
        {
            testLatitude2 = latitude;
            testLongitude2 = longitude;
        }
        
        LogDebug($"Updated test coordinates: {latitude}, {longitude}");
        UpdateDebugDisplay();
    }
    
    /// <summary>
    /// Toggles between first and second test location
    /// </summary>
    public void ToggleTestLocation()
    {
        useFirstLocation = !useFirstLocation;
        LogDebug($"Switched to {(useFirstLocation ? "first" : "second")} test location");
        UpdateDebugDisplay();
    }
    
    #region Debug Logging
    
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[MapTestingScript] {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[MapTestingScript] {message}");
    }
    
    #endregion
    
    #region Public API for External Testing
    
    /// <summary>
    /// Simulates guess submission with custom coordinates
    /// </summary>
    public void TestGuessSubmit(float latitude, float longitude)
    {
        if (MapInteractionManager.Instance == null)
        {
            LogError("MapInteractionManager.Instance is null!");
            return;
        }
        
        string jsonData = $"{{\"latitude\": {latitude}, \"longitude\": {longitude}, \"timestamp\": {System.DateTimeOffset.Now.ToUnixTimeSeconds()}}}";
        MapInteractionManager.Instance.SubmitGuess(jsonData);
        LogDebug($"Custom guess submit simulated at: {latitude}, {longitude}");
    }
    
    #endregion
}
