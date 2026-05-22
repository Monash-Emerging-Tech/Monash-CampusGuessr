using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;


/// <summary>
/// Controls the UI elements related to map interaction
/// Handles map scaling, animation, button controls, and guess information
/// Merged functionality from Expand_Close_Minimap
/// 
/// Written by aleu0007
/// Last Modified: 24/09/2025
/// </summary>
public class MapUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button submitGuessButton;
    [SerializeField] private TextMeshProUGUI guessInfoText;
    [SerializeField] private GameObject mapOverlay; // Semi-transparent overlay that appears when map is active (from WebGL template)
    [SerializeField] private GuessButtonManager guessButtonManager;

    [Header("Map Scaling")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform mapContainer;
    [SerializeField] private RectTransform primaryButton;
    [SerializeField] private RectTransform cornerButtons; // Parent object containing all corner buttons
    [SerializeField] private Button expandButton;
    [SerializeField] private Button minimizeButton;
    [SerializeField] private Button pinButton;
    [SerializeField] private Vector3 minimizedScale = new Vector3(1.0f, 1.0f, 1.0f);   // Original size (440x230)
    [SerializeField] private Vector3 intermediateScale = new Vector3(2.5f, 2.5f, 2.5f); // Medium size (1100x575)
    [SerializeField] private Vector3 expandedScale = new Vector3(3.0f, 3.0f, 3.0f);     // Large size (1320x690) - smaller than fullscreen
    [SerializeField] private float scaleSpeed = 5f;

    [Header("Pin Button Sprites")]
    [SerializeField] private Sprite pinActiveSprite;    // Sprite when pin is active (pinned)
    [SerializeField] private Sprite pinInactiveSprite;  // Sprite when pin is inactive (not pinned)

    [Header("Map Controls Visibility")]
    [SerializeField] private GameObject[] mapControlObjects; // Objects to hide/show during guessing

    // Private state - controlled by pin button clicks
    private bool isPinned = false;

    [Header("Settings")]
    [SerializeField] private string guessFormat = "Guess placed at: {0:F6}, {1:F6}";


    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // State
#nullable enable
    private MapInteractionManager.LocationData? currentGuess = null;
#nullable disable

    // Scaling state
    private Vector3 targetScale;
    private float targetWidth;

    // Map size states
    public enum MapSize { Minimized, Intermediate, Expanded, Fullscreen }
    private MapSize currentMapSize = MapSize.Minimized;

    private void Start()
    {
        // Subscribe to map events only
        MapInteractionManager.OnGuessSubmitted += OnGuessSubmitted;
        MapInteractionManager.OnMapOpened += OnMapOpened;
        MapInteractionManager.OnMapClosed += OnMapClosed;

        // Subscribe to game events for scaling
        GameLogic.OnRoundStarted += OnRoundStarted;
        GameLogic.OnRoundEnded += OnRoundEnded;

        // Subscribe to guess button events
        GuessButtonManager.OnGuessSubmitted += OnGuessSubmitted;
        GuessButtonManager.OnNextRoundRequested += OnNextRoundRequested;

        // Setup button listeners
        SetupButtonListeners();

        // Initialize map scaling
        InitializeMapScaling();

        // Initialize map UI
        UpdateMapUI();

        // Initialize pin button state
        UpdatePinButton();

        // Initialize button states
        UpdateButtonStates(mapContainer != null ? mapContainer.localScale.x : minimizedScale.x);

        LogDebug("MapUIController initialized");
    }

    private void OnDestroy()
    {
        // Unsubscribe from map events
        MapInteractionManager.OnGuessSubmitted -= OnGuessSubmitted;
        MapInteractionManager.OnMapOpened -= OnMapOpened;
        MapInteractionManager.OnMapClosed -= OnMapClosed;

        // Unsubscribe from game events
        GameLogic.OnRoundStarted -= OnRoundStarted;
        GameLogic.OnRoundEnded -= OnRoundEnded;

        // Unsubscribe from guess button events
        GuessButtonManager.OnGuessSubmitted -= OnGuessSubmitted;
        GuessButtonManager.OnNextRoundRequested -= OnNextRoundRequested;
    }

    private void Update()
    {
        UpdateMapScaling();

        // Check for clicks outside the minimap
        if (Input.GetMouseButtonDown(0))
        {
            bool isClickOnUI = EventSystem.current != null ? EventSystem.current.IsPointerOverGameObject() : false;
            bool isClickOnMinimap = IsClickOnMinimap();
            bool isClickOnCornerButtons = IsClickOnCornerButtons();

            if (isClickOnUI)
            {
                // Click is on UI - check if it's on the minimap or corner buttons
                if (!isClickOnMinimap && !isClickOnCornerButtons)
                {
                    // Clicked on UI but not on minimap or corner buttons
                    if (!isPinned)
                    {
                        MinimizeMap();
                    }
                    else if (isPinned)
                    {
                        // Only log when pin is active and click is outside
                        LogDebug("Pin is active - Click outside map/corner buttons ignored");
                    }
                }
            }
            else
            {
                // Click is not on any UI (clicked in 3D world)
                if (!isPinned)
                {
                    MinimizeMap();
                }
                else if (isPinned)
                {
                    // Only log when pin is active and click is outside
                    LogDebug("Pin is active - Click in 3D world ignored");
                }
            }
        }
    }

    /// <summary>
    /// Sets up button event listeners
    /// </summary>
    private void SetupButtonListeners()
    {
        if (submitGuessButton != null)
        {
            submitGuessButton.onClick.AddListener(OnSubmitGuessClicked);
        }

        if (pinButton != null)
        {
            pinButton.onClick.AddListener(OnPinButtonClicked);
        }

        if (expandButton != null)
        {
            expandButton.onClick.AddListener(OnExpandButtonClicked);
        }

        if (minimizeButton != null)
        {
            minimizeButton.onClick.AddListener(OnMinimizeButtonClicked);
        }
    }


    /// <summary>
    /// Called when submit guess button is clicked
    /// </summary>
    private void OnSubmitGuessClicked()
    {
        if (MapInteractionManager.Instance != null)
        {
            // This will trigger the JavaScript function to submit the guess
            MapInteractionManager.Instance.HideMap();
        }
    }

    /// <summary>
    /// Called when guess button is clicked (for scaling)
    /// </summary>
    public void OnGuessButtonClicked()
    {
        if (GameLogic.Instance != null && GameLogic.Instance.IsGuessing())
        {
            // When guessing, always expand regardless of pin state
            ExpandMap(true); // Fullscreen when guessing

            // Hide map controls during guessing (only when actually submitting guess)
            HideMapControls();

            LogDebug("Guess button clicked - Map expanded for guessing, controls hidden (pin overridden)");
        }
        else
        {
            // Only minimize if not pinned
            if (!isPinned)
            {
                MinimizeMap();
                LogDebug("Guess button clicked - Map minimized (not pinned)");
            }
            else
            {
                LogDebug("Guess button clicked - Map not minimized (pinned)");
            }
        }
    }

    /// <summary>
    /// Called when pin button is clicked
    /// </summary>
    private void OnPinButtonClicked()
    {
        TogglePin();
    }

    /// <summary>
    /// Called when expand button is clicked
    /// </summary>
    private void OnExpandButtonClicked()
    {
        if (!expandButton.interactable) return; // Don't act if disabled

        // Cycle through sizes: Minimized -> Intermediate -> Expanded
        switch (currentMapSize)
        {
            case MapSize.Minimized:
                SetMapSize(MapSize.Intermediate);
                LogDebug("Expand button clicked - Map set to intermediate size");
                break;
            case MapSize.Intermediate:
                SetMapSize(MapSize.Expanded);
                LogDebug("Expand button clicked - Map set to expanded size");
                break;
            case MapSize.Expanded:
                // Already at max size, no action
                LogDebug("Expand button clicked - Already at maximum size");
                break;
            case MapSize.Fullscreen:
                // From fullscreen, go to expanded
                SetMapSize(MapSize.Expanded);
                LogDebug("Expand button clicked - Map set from fullscreen to expanded");
                break;
        }
    }

    /// <summary>
    /// Called when minimize button is clicked
    /// </summary>
    private void OnMinimizeButtonClicked()
    {
        if (!minimizeButton.interactable) return; // Don't act if disabled

        // Cycle through sizes: Expanded -> Intermediate -> Minimized
        switch (currentMapSize)
        {
            case MapSize.Expanded:
                SetMapSize(MapSize.Intermediate);
                LogDebug("Minimize button clicked - Map set to intermediate size");
                break;
            case MapSize.Intermediate:
                SetMapSize(MapSize.Minimized);
                LogDebug("Minimize button clicked - Map set to minimized size");
                break;
            case MapSize.Minimized:
                // Already at min size, no action
                LogDebug("Minimize button clicked - Already at minimum size");
                break;
            case MapSize.Fullscreen:
                // From fullscreen, go to expanded
                SetMapSize(MapSize.Expanded);
                LogDebug("Minimize button clicked - Map set from fullscreen to expanded");
                break;
        }
    }


    /// <summary>
    /// Shows the map interface
    /// </summary>
    private void ShowMap()
    {
        if (MapInteractionManager.Instance != null)
        {
            MapInteractionManager.Instance.ShowMap();
        }
    }

    /// <summary>
    /// Hides the map interface
    /// </summary>
    private void HideMap()
    {
        if (MapInteractionManager.Instance != null)
        {
            MapInteractionManager.Instance.HideMap();
        }
    }

    /// <summary>
    /// Updates map-related UI elements
    /// </summary>
    private void UpdateMapUI()
    {
        UpdateGuessInfo();
    }

    /// <summary>
    /// Updates the guess information display
    /// </summary>
    private void UpdateGuessInfo()
    {
        if (guessInfoText != null)
        {
            if (currentGuess != null)
            {
                guessInfoText.text = string.Format(guessFormat,
                    currentGuess.latitude, currentGuess.longitude);
                guessInfoText.gameObject.SetActive(true);
            }
            else
            {
                guessInfoText.gameObject.SetActive(false);
            }
        }
    }


    // Event handlers


    private void OnGuessSubmitted(MapInteractionManager.LocationData guessLocation)
    {
        currentGuess = guessLocation;
        UpdateGuessInfo();
        ExpandMap(true);
        HideMapControls();
        LogDebug($"Guess submitted at {guessLocation.latitude}, {guessLocation.longitude}, Level: {guessLocation.zLevelName} - UI updated");
    }

    private void OnMapOpened()
    {
        if (mapOverlay != null)
        {
            mapOverlay.SetActive(true);
        }

        LogDebug("Map opened - UI updated");
    }

    private void OnMapClosed()
    {
        if (mapOverlay != null)
        {
            mapOverlay.SetActive(false);
        }

        LogDebug("Map closed - UI updated");
    }

    private void OnRoundStarted(int round)
    {
        // Reset pin state at start of each round
        isPinned = false;
        UpdatePinButton();

        // Ensure map controls are visible at start of round
        ShowMapControls();

        // Minimize map when new round starts (pin is now false, so this will work)
        MinimizeMap();
        LogDebug($"Round {round} started - Pin reset, Map minimized, controls shown");
    }

    private void OnRoundEnded(int score)
    {
        // Expand map when round ends to show results
        ExpandMap(true);
        LogDebug($"Round ended with score {score} - Map expanded");
    }

    private void OnGuessSubmitted()
    {
        // Handle guess submission - expand map to fullscreen
        ExpandMap(true);
        HideMapControls();

        LogDebug("Guess submitted - Map expanded to fullscreen");
    }

    private void OnNextRoundRequested()
    {
        // Handle next round request - minimize map
        MinimizeMap();

        // Show map controls again for next round
        ShowMapControls();

        LogDebug("Next round requested - Map minimized, controls shown");
    }

    // Public methods for external access


    public void SetSubmitButtonInteractable(bool interactable)
    {
        if (submitGuessButton != null)
        {
            submitGuessButton.interactable = interactable;
        }
    }

    public void ShowGuessInfo(bool show)
    {
        if (guessInfoText != null)
        {
            guessInfoText.gameObject.SetActive(show);
        }
    }

    /// <summary>
    /// Toggles the pin state of the minimap
    /// </summary>
    public void TogglePin()
    {
        isPinned = !isPinned;
        UpdatePinButton();
        LogDebug($"Minimap pin {(isPinned ? "activated" : "deactivated")}");
    }

    /// <summary>
    /// Sets the pin state of the minimap
    /// </summary>
    /// <param name="pinned">Whether the minimap should be pinned</param>
    public void SetPinned(bool pinned)
    {
        isPinned = pinned;
        UpdatePinButton();
        LogDebug($"Minimap pin {(isPinned ? "activated" : "deactivated")}");
    }

    /// <summary>
    /// Gets the current pin state
    /// </summary>
    /// <returns>True if minimap is pinned</returns>
    public bool IsPinned()
    {
        return isPinned;
    }

    /// <summary>
    /// Updates the pin button appearance based on current state
    /// </summary>
    private void UpdatePinButton()
    {
        if (pinButton == null)
        {
            LogDebug("UpdatePinButton called but pinButton is null");
            return;
        }

        // Get the button's image component
        Image buttonImage = pinButton.GetComponent<Image>();
        if (buttonImage == null)
        {
            LogDebug("UpdatePinButton called but pinButton has no Image component");
            return;
        }

        // Set the appropriate sprite based on pin state
        Sprite targetSprite = isPinned ? pinActiveSprite : pinInactiveSprite;
        if (targetSprite != null)
        {
            buttonImage.sprite = targetSprite;
            LogDebug($"Pin button sprite changed to: {targetSprite.name}");
        }
        else
        {
            LogDebug($"Pin button sprite is null for state: {(isPinned ? "Active" : "Inactive")}");
        }

        // Configure button colors to have no hover effects (all states use same color)
        var colors = pinButton.colors;
        Color buttonColor = isPinned ? Color.yellow : Color.white;

        // Set all color states to the same color (no hover effects)
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonColor;  // No hover effect
        colors.pressedColor = buttonColor;      // No press effect
        colors.selectedColor = buttonColor;     // No selection effect
        colors.disabledColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.5f); // Slightly transparent when disabled

        pinButton.colors = colors;

        LogDebug($"Pin button updated - State: {(isPinned ? "Active" : "Inactive")}, Sprite: {(targetSprite != null ? targetSprite.name : "null")}, Color: {buttonColor}");
    }

    #region Map Scaling Methods

    /// <summary>
    /// Initializes map scaling to minimized state
    /// </summary>
    private void InitializeMapScaling()
    {
        if (mapContainer != null)
        {
            currentMapSize = MapSize.Minimized;
            targetScale = minimizedScale; // Uniform scaling - original size
            mapContainer.localScale = targetScale;
            targetWidth = mapContainer.rect.width * targetScale.x;
        }

        // Setup primary button size
        if (primaryButton != null)
        {
            primaryButton.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        }

        // Setup corner buttons group
        if (cornerButtons != null)
        {
            // Use expanded size for corner buttons group
            float expandedWidth = cornerButtons.rect.width;
            float expandedHeight = cornerButtons.rect.height;
            cornerButtons.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, expandedWidth);
            cornerButtons.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, expandedHeight);
        }

        LogDebug("Map scaling initialized");
    }

    /// <summary>
    /// Updates map scaling animation and button positioning
    /// </summary>
    private void UpdateMapScaling()
    {
        if (mapContainer == null) return;

        // Animate map container scaling
        if (mapContainer.localScale != targetScale)
        {
            Vector3 oldScale = mapContainer.localScale;
            mapContainer.localScale = Vector3.Lerp(mapContainer.localScale, targetScale, Time.deltaTime * scaleSpeed);

            // Debug logging for expanded state only
            if (currentMapSize == MapSize.Expanded)
            {
                LogDebug($"Expanded scaling: Old={oldScale} -> New={mapContainer.localScale} (Target={targetScale})");
            }
        }

        // Update primary button size and position
        UpdatePrimaryButton();

        // Update corner buttons position
        UpdateCornerButtons();
    }

    /// <summary>
    /// Updates primary button size and alignment
    /// </summary>
    private void UpdatePrimaryButton()
    {
        if (primaryButton == null || mapContainer == null) return;

        float currentWidth = primaryButton.rect.width;
        if (Mathf.Abs(currentWidth - targetWidth) > 0.01f)
        {
            float newWidth = Mathf.Lerp(currentWidth, targetWidth, Time.deltaTime * scaleSpeed);
            primaryButton.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
        }

        // Align right edge
        float scaleX = mapContainer.localScale.x;
        float mapRight = mapContainer.anchoredPosition.x + (mapContainer.rect.width * (1 - mapContainer.pivot.x)) * scaleX;
        float buttonRight = primaryButton.anchoredPosition.x + (primaryButton.rect.width * (1 - primaryButton.pivot.x));
        float offset = mapRight - buttonRight;
        primaryButton.anchoredPosition = new Vector2(primaryButton.anchoredPosition.x + offset, primaryButton.anchoredPosition.y);
    }

    /// <summary>
    /// Updates corner buttons group positioning and individual button states
    /// </summary>
    private void UpdateCornerButtons()
    {
        if (mapContainer == null || cornerButtons == null) return;

        float scaleX = mapContainer.localScale.x;
        float scaleY = mapContainer.localScale.y;

        // MapContainer left edge
        float mapLeft = mapContainer.anchoredPosition.x - (mapContainer.rect.width * mapContainer.pivot.x) * scaleX;
        // MapContainer top edge
        float mapTop = mapContainer.anchoredPosition.y + (mapContainer.rect.height * (1 - mapContainer.pivot.y)) * scaleY;

        // Position the entire corner-buttons group
        float cornerPivotX = cornerButtons.pivot.x;
        float cornerPivotY = cornerButtons.pivot.y;
        float newX = mapLeft + cornerButtons.rect.width * cornerPivotX;
        float newY = mapTop + cornerButtons.rect.height * cornerPivotY;
        cornerButtons.anchoredPosition = new Vector2(newX, newY);

        // Update button states - all buttons always visible
        UpdateButtonStates(scaleX);
    }

    /// <summary>
    /// Updates the interactable state of expand and minimize buttons
    /// </summary>
    private void UpdateButtonStates(float currentScaleX)
    {
        // Check if currently guessing
        bool isGuessing = GameLogic.Instance != null && GameLogic.Instance.IsGuessing();

        // Update expand button state - enabled when not at maximum size (regardless of guessing state)
        if (expandButton != null)
        {
            bool canExpand = (currentMapSize == MapSize.Minimized || currentMapSize == MapSize.Intermediate || currentMapSize == MapSize.Fullscreen);
            expandButton.interactable = canExpand;
        }

        // Update minimize button state - enabled when not at minimum size (regardless of guessing state)
        if (minimizeButton != null)
        {
            bool canMinimize = (currentMapSize == MapSize.Expanded || currentMapSize == MapSize.Intermediate || currentMapSize == MapSize.Fullscreen);
            minimizeButton.interactable = canMinimize;
        }

        // Update pin button state - enabled when guessing
        if (pinButton != null)
        {
            bool wasInteractable = pinButton.interactable;
            pinButton.interactable = isGuessing;

            if (wasInteractable != pinButton.interactable)
            {
                // Update pin button appearance when interactable state changes
                UpdatePinButton();
            }
        }
    }

    /// <summary>
    /// Hides map controls when guess button is clicked (replaces HideMapControls script)
    /// </summary>
    public void HideMapControls()
    {
        // Hide map control objects
        if (mapControlObjects != null)
        {
            foreach (GameObject controlObject in mapControlObjects)
            {
                if (controlObject != null)
                {
                    controlObject.SetActive(false);
                }
            }
        }

        // Hide corner buttons
        if (cornerButtons != null)
        {
            cornerButtons.gameObject.SetActive(false);
        }

        // Hide submit guess button during results state
        if (submitGuessButton != null)
        {
            submitGuessButton.gameObject.SetActive(false);
        }

        LogDebug("Map controls hidden - Guess button clicked");
    }

    /// <summary>
    /// Shows map controls when guessing ends
    /// </summary>
    public void ShowMapControls()
    {
        // Show map control objects
        if (mapControlObjects != null)
        {
            foreach (GameObject controlObject in mapControlObjects)
            {
                if (controlObject != null)
                {
                    controlObject.SetActive(true);
                }
            }
        }

        // Show corner buttons
        if (cornerButtons != null)
        {
            cornerButtons.gameObject.SetActive(true);
        }

        // Show submit guess button during active guessing state
        if (submitGuessButton != null)
        {
            submitGuessButton.gameObject.SetActive(true);
        }

        LogDebug("Map controls shown");
    }

    /// <summary>
    /// Sets the map to a specific size
    /// </summary>
    /// <param name="size">The size to set the map to</param>
    private void SetMapSize(MapSize size)
    {
        currentMapSize = size;

        switch (size)
        {
            case MapSize.Minimized:
                targetScale = minimizedScale; // Uniform scaling - maintains 440x230 aspect ratio
                LogDebug($"SetMapSize: Minimized - Scale: {targetScale}");
                break;
            case MapSize.Intermediate:
                targetScale = intermediateScale; // Uniform scaling - maintains 1100x575 aspect ratio
                LogDebug($"SetMapSize: Intermediate - Scale: {targetScale}");
                break;
            case MapSize.Expanded:
                // Force uniform scaling to ensure correct aspect ratio
                // Something before this step messes up the aspect ratio
                targetScale = new Vector3(4.0f, 4.0f, 4.0f);
                LogDebug($"SetMapSize: Expanded - Scale: {targetScale} (forced uniform scaling)");
                break;
            case MapSize.Fullscreen:
                targetScale = GetFullscreenScale();
                LogDebug($"SetMapSize: Fullscreen - Scale: {targetScale} (calculated dynamically)");
                break;
        }

        if (mapContainer != null)
        {
            targetWidth = mapContainer.rect.width * targetScale.x;
        }

        // Align right edge after width is set
        if (primaryButton != null && mapContainer != null)
        {
            float mapRight = mapContainer.anchoredPosition.x + (mapContainer.rect.width * (1 - mapContainer.pivot.x)) * targetScale.x;
            float buttonRight = primaryButton.anchoredPosition.x + (primaryButton.rect.width * (1 - primaryButton.pivot.x)) * targetScale.x;
            float offset = mapRight - buttonRight;
            primaryButton.anchoredPosition = new Vector2(primaryButton.anchoredPosition.x + offset, primaryButton.anchoredPosition.y);
        }

        LogDebug($"Map size set to: {size}");

        // Additional debug for expanded state
        if (size == MapSize.Expanded)
        {
            LogDebug($"Expanded debug - TargetScale: {targetScale}, Inspector expandedScale: {expandedScale}, MapContainer scale will be: {targetScale}");
        }
    }

    /// <summary>
    /// Minimizes the map to small size
    /// </summary>
    public void MinimizeMap()
    {
        SetMapSize(MapSize.Minimized);
    }

    /// <summary>
    /// Expands the map to large or fullscreen size
    /// </summary>
    /// <param name="fullscreen">Whether to use fullscreen scale</param>
    public void ExpandMap(bool fullscreen = false)
    {
        SetMapSize(fullscreen ? MapSize.Fullscreen : MapSize.Expanded);
    }

    /// <summary>
    /// Calculates fullscreen scale based on canvas resolution
    /// </summary>
    /// <returns>Fullscreen scale vector</returns>
    private Vector3 GetFullscreenScale()
    {
        if (mapContainer == null || canvas == null) return expandedScale;

        RectTransform rectTransform = mapContainer.GetComponent<RectTransform>();
        CanvasScaler canvasScaler = canvas.GetComponent<CanvasScaler>();

        Vector3 fullscreenScale = new Vector3(
            ((canvasScaler.referenceResolution.x - 30) / rectTransform.rect.width),
            (canvasScaler.referenceResolution.y / rectTransform.rect.height) * 80f / 100f,
            ((canvasScaler.referenceResolution.y / rectTransform.rect.height) * 80f / 100f)
        );

        LogDebug($"GetFullscreenScale calculated: {fullscreenScale} (Canvas: {canvasScaler.referenceResolution}, Container: {rectTransform.rect.width}x{rectTransform.rect.height})");
        return fullscreenScale;
    }

    #endregion

    /// <summary>
    /// Checks if the current mouse click is on the minimap
    /// </summary>
    /// <returns>True if click is on minimap, false otherwise</returns>
    private bool IsClickOnMinimap()
    {
        if (mapContainer == null) return false;

        Vector2 mousePosition = Input.mousePosition;
        return RectTransformUtility.RectangleContainsScreenPoint(mapContainer, mousePosition);
    }

    /// <summary>
    /// Checks if the current mouse click is on the corner buttons area
    /// </summary>
    /// <returns>True if click is on corner buttons, false otherwise</returns>
    private bool IsClickOnCornerButtons()
    {
        if (cornerButtons == null) return false;

        Vector2 mousePosition = Input.mousePosition;
        return RectTransformUtility.RectangleContainsScreenPoint(cornerButtons, mousePosition);
    }

    // Debug logging methods
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[MapUIController] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[MapUIController] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[MapUIController] {message}");
    }
}
