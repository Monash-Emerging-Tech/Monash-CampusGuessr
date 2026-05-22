using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the guess button states and appearance
/// Handles button text, colors, and interactability based on game state
/// 
/// Written by Assistant
/// Last Modified: 24/09/2025
/// </summary>
public class GuessButtonManager : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private Button guessButton;
    [SerializeField] private TMP_Text textReference;
    [SerializeField] private Image buttonImage;

    [Header("Button Colors")]
    [SerializeField] private Color waitingNormalColor = Color.red; // Disabled state
    [SerializeField] private Color readyNormalColor = Color.blue;
    [SerializeField] private Color readyHoverColor = new Color(0f, 0.4f, 1f, 1f);
    [SerializeField] private Color resultsNormalColor = Color.red;
    [SerializeField] private Color resultsHoverColor = new Color(1f, 0.4f, 0f, 1f);

    [Header("Waiting State Settings")]
    [SerializeField][Range(0f, 1f)] private float waitingOpacity = 0.5f; // Opacity for waiting state

    [Header("Button Text")]
    [SerializeField] private string waitingText = "Place your pin on the map";
    [SerializeField] private string readyText = "Guess";
    [SerializeField] private string resultsText = "Next";

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // State management
    public enum GuessButtonState
    {
        WaitingForPin,    // Red, disabled, "Place your pin on the map"
        ReadyToGuess,     // Blue, enabled, "Guess"
        Results           // Hidden or different UI
    }

    private GuessButtonState currentState = GuessButtonState.WaitingForPin;
    private GameLogic gameLogic;
    private MapUIController mapUIController;
    private bool isInitializing = true;

    // Events
    public static event Action<GuessButtonState> OnStateChanged;
    public static event Action OnGuessSubmitted;
    public static event Action OnNextRoundRequested;

    #region Unity Lifecycle

    void Start()
    {
        // Get references
        gameLogic = GameObject.Find("GameLogic")?.GetComponent<GameLogic>();
#if UNITY_2023_1_OR_NEWER
        mapUIController = UnityEngine.Object.FindFirstObjectByType<MapUIController>();
#else
#pragma warning disable 618
		mapUIController = FindObjectOfType<MapUIController>();
#pragma warning restore 618
#endif

        // Get button components if not assigned
        if (guessButton == null)
            guessButton = GetComponent<Button>();
        if (textReference == null)
            textReference = GetComponentInChildren<TMP_Text>();
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        // Check for other scripts that might be managing this button
        var otherButtonManagers = GetComponents<MonoBehaviour>();
        foreach (var script in otherButtonManagers)
        {
            if (script != this && script.GetType().Name.Contains("Button"))
            {
                LogDebug($"WARNING: Found other button script: {script.GetType().Name}");
            }
        }

        // Setup button listener
        if (guessButton != null)
        {
            guessButton.onClick.AddListener(OnButtonClicked);
        }

        // Subscribe to map events
        // Note: OnPinPlaced removed - button state now managed by HTML button
        MapInteractionManager.OnGuessSubmitted += OnMapGuessSubmitted;

        // Initialize state to waiting
        InitializeToWaitingState();

        // Ensure button state is set after a frame (in case other scripts override it)
        StartCoroutine(EnsureWaitingStateAfterFrame());

        LogDebug("GuessButtonManager initialized");
    }

    void Update()
    {
        // Only check for overrides during initialization
        if (isInitializing && currentState == GuessButtonState.WaitingForPin && guessButton != null && guessButton.interactable)
        {
            LogDebug("WARNING: Button is in waiting state but is interactable! Resetting...");
            // Force the button to be non-interactable
            guessButton.interactable = false;
            // Also ensure the colors are correct
            UpdateButtonAppearance();
        }
    }

    void OnDestroy()
    {
        if (guessButton != null)
        {
            guessButton.onClick.RemoveListener(OnButtonClicked);
        }

        // Unsubscribe from map events
        // Note: OnPinPlaced removed - button state now managed by HTML button
        MapInteractionManager.OnGuessSubmitted -= OnMapGuessSubmitted;
    }

    #endregion

    #region State Management

    /// <summary>
    /// Sets the current button state and updates appearance
    /// </summary>
    /// <param name="newState">The new state to set</param>
    public void SetState(GuessButtonState newState)
    {
        if (currentState == newState) return;

        GuessButtonState previousState = currentState;
        currentState = newState;

        UpdateButtonAppearance();
        OnStateChanged?.Invoke(newState);

        LogDebug($"State changed from {previousState} to {newState}");
    }

    /// <summary>
    /// Gets the current button state
    /// </summary>
    /// <returns>Current button state</returns>
    public GuessButtonState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Updates button appearance based on current state
    /// </summary>
    private void UpdateButtonAppearance()
    {
        if (guessButton == null || textReference == null) return;

        switch (currentState)
        {
            case GuessButtonState.WaitingForPin:
                // Disabled, red with lowered opacity, no hover effects
                guessButton.interactable = false;
                UpdateButtonColorsWithOpacity(waitingNormalColor, waitingOpacity, false);
                textReference.text = waitingText;
                // Force the button to be non-interactable again (in case something overrides it)
                guessButton.interactable = false;
                break;

            case GuessButtonState.ReadyToGuess:
                // Enabled, blue, with hover effects
                guessButton.interactable = true;
                UpdateButtonColors(readyNormalColor, readyHoverColor);
                textReference.text = readyText;
                break;

            case GuessButtonState.Results:
                // Enabled, red, with hover effects
                guessButton.interactable = true;
                UpdateButtonColors(resultsNormalColor, resultsHoverColor);
                textReference.text = resultsText;
                break;
        }

        LogDebug($"Button updated - State: {currentState}, Interactable: {guessButton.interactable}, Text: {textReference.text}");
    }

    /// <summary>
    /// Updates button colors using Unity's color tint system
    /// </summary>
    /// <param name="normalColor">The normal color for the button</param>
    /// <param name="hoverColor">The hover color for the button</param>
    private void UpdateButtonColors(Color normalColor, Color hoverColor)
    {
        if (guessButton == null) return;

        var colors = guessButton.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = normalColor * 0.8f; // 20% darker for press
        colors.selectedColor = normalColor;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Default disabled color

        guessButton.colors = colors;
    }


    /// <summary>
    /// Updates button colors with opacity control (for waiting state)
    /// </summary>
    /// <param name="normalColor">The normal color for the button</param>
    /// <param name="opacity">The opacity/alpha value (0-1)</param>
    /// <param name="enableHoverEffects">Whether to enable hover and press effects</param>
    private void UpdateButtonColorsWithOpacity(Color normalColor, float opacity, bool enableHoverEffects)
    {
        if (guessButton == null) return;

        // Apply opacity to the normal color
        Color normalWithOpacity = new Color(normalColor.r, normalColor.g, normalColor.b, opacity);

        var colors = guessButton.colors;
        colors.normalColor = normalWithOpacity;

        if (enableHoverEffects)
        {
            // Enable hover and press effects with opacity
            colors.highlightedColor = normalWithOpacity * 1.2f; // 20% brighter for hover
            colors.pressedColor = normalWithOpacity * 0.8f; // 20% darker for press
            colors.selectedColor = normalWithOpacity;
        }
        else
        {
            // Disable hover and press effects (all same color with opacity)
            colors.highlightedColor = normalWithOpacity;
            colors.pressedColor = normalWithOpacity;
            colors.selectedColor = normalWithOpacity;
        }

        // Disabled color with opacity - use the same color, not grey
        colors.disabledColor = normalWithOpacity;

        guessButton.colors = colors;

        // Also directly set the button image color to ensure it shows correctly
        if (buttonImage != null)
        {
            buttonImage.color = normalWithOpacity;
        }
    }

    #endregion

    #region Button Actions

    /// <summary>
    /// Called when the guess button is clicked
    /// </summary>
    private void OnButtonClicked()
    {
        LogDebug($"Button clicked - Current state: {currentState}");

        switch (currentState)
        {
            case GuessButtonState.WaitingForPin:
                // Button should be disabled, but handle just in case
                LogDebug("Button clicked while waiting for pin - no action");
                break;

            case GuessButtonState.ReadyToGuess:
                SubmitGuess();
                break;

            case GuessButtonState.Results:
                RequestNextRound();
                break;
        }
    }

    /// <summary>
    /// Handles guess submission
    /// </summary>
    private void SubmitGuess()
    {
        LogDebug("Submitting guess...");

        // Trigger map expansion through MapUIController
        if (mapUIController != null)
        {
            mapUIController.OnGuessButtonClicked();
        }

        // Submit guess through GameLogic
        if (gameLogic != null)
        {
            gameLogic.submitGuess();
        }

        // Change to results state
        SetState(GuessButtonState.Results);

        // Trigger event
        OnGuessSubmitted?.Invoke();

        LogDebug("Guess submitted successfully");
    }

    /// <summary>
    /// Handles next round request
    /// </summary>
    private void RequestNextRound()
    {
        LogDebug("Requesting next round...");

        // Start next round through GameLogic
        if (gameLogic != null)
        {
            gameLogic.nextRound();
        }

        // Reset to waiting state
        SetState(GuessButtonState.WaitingForPin);

        // Trigger event
        OnNextRoundRequested?.Invoke();

        LogDebug("Next round requested");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Called when a pin is placed on the map
    /// </summary>
    public void OnPinPlaced()
    {
        if (currentState == GuessButtonState.WaitingForPin)
        {
            SetState(GuessButtonState.ReadyToGuess);
            LogDebug("Pin placed - Button ready for guess");
        }
    }

    /// <summary>
    /// Called when a new round starts
    /// </summary>
    public void OnRoundStarted()
    {
        SetState(GuessButtonState.WaitingForPin);
        LogDebug("Round started - Button reset to waiting state");
    }

    /// <summary>
    /// Ensures button is in waiting state (called on Start)
    /// </summary>
    public void InitializeToWaitingState()
    {
        SetState(GuessButtonState.WaitingForPin);
        LogDebug("Button initialized to waiting state");
    }

    /// <summary>
    /// Coroutine to ensure button is in waiting state after other scripts have run
    /// </summary>
    private IEnumerator EnsureWaitingStateAfterFrame()
    {
        yield return null; // Wait one frame

        if (currentState != GuessButtonState.WaitingForPin)
        {
            LogDebug("Button state was overridden, resetting to waiting state");
            SetState(GuessButtonState.WaitingForPin);
        }

        yield return new WaitForSeconds(0.1f); // Wait a bit more

        if (currentState != GuessButtonState.WaitingForPin)
        {
            LogDebug("Button state was overridden again, forcing waiting state");
            SetState(GuessButtonState.WaitingForPin);
        }

        // Mark initialization as complete
        isInitializing = false;
        LogDebug("Button initialization complete");
    }

    /// <summary>
    /// Called when a round ends
    /// </summary>
    public void OnRoundEnded()
    {
        SetState(GuessButtonState.Results);
        LogDebug("Round ended - Button shows results state");
    }

    /// <summary>
    /// Called when a guess is submitted through the map (from MapInteractionManager)
    /// </summary>
    private void OnMapGuessSubmitted(MapInteractionManager.LocationData guessLocation)
    {
        SetState(GuessButtonState.Results);
        LogDebug($"Map guess submitted at {guessLocation.latitude}, {guessLocation.longitude}, Level: {guessLocation.zLevelName} - Button shows results state");
    }

    #endregion

    #region Debug Logging

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[GuessButtonManager] {message}");
        }
    }

    #endregion
}
