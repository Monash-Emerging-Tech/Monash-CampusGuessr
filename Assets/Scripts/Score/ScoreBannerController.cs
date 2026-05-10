using UnityEngine;
using TMPro;

/// <summary>
/// Controls the ScoreBanner prefab UI elements (score, round display, map pack info)
/// Subscribes to GameLogic events for updates
/// 
/// Written by aleu0007
/// Last Modified: 26/09/2025
/// </summary>
public class ScoreBannerController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private TextMeshProUGUI mapPackText;
    [SerializeField] private RectTransform backgroundRect; // Background child object
    [SerializeField] private RectTransform containerRect; // Main container (this object)
    [SerializeField] private RectTransform mapTextContainerRect; // Map text container (left-anchored)
    
    [Header("Settings")]
    [SerializeField] private string scoreFormat = "{0}";
    [SerializeField] private string roundFormat = "{0}/{1}";
    [SerializeField] private string mapPackFormat = "{0}";
    
    [Header("Dynamic Resizing")]
    [SerializeField] private bool enableDynamicResizing = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // Private fields for resizing
    private float originalWidth;
    private float trueOriginalWidth; // Store the actual original width before minimum enforcement
    private Vector2 originalAnchoredPosition;
    private bool isInitialized = false;
    
    // Dynamic sizing values
    private float dynamicPadding = 20f;
    private float dynamicSafetyMargin = 20f;
    
    // Minimum width to prevent squishing
    private const float MINIMUM_WIDTH = 300f;
    
    private void Start()
    {
        // Subscribe to GameLogic events
        GameLogic.OnScoreUpdated += OnScoreUpdated;
        GameLogic.OnRoundUpdated += OnRoundUpdated;
        GameLogic.OnGameEnded += OnGameEnded;
        GameLogic.OnMapPackChanged += OnMapPackChanged;
        
        // Initialize resizing system
        InitializeResizing();
        
        // Initialize UI
        UpdateUI();
        
        LogDebug("ScoreBannerController initialized");
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from GameLogic events
        GameLogic.OnScoreUpdated -= OnScoreUpdated;
        GameLogic.OnRoundUpdated -= OnRoundUpdated;
        GameLogic.OnGameEnded -= OnGameEnded;
        GameLogic.OnMapPackChanged -= OnMapPackChanged;
    }
    
    /// <summary>
    /// Updates all UI elements
    /// </summary>
    private void UpdateUI()
    {
        UpdateScoreDisplay();
        UpdateRoundDisplay();
        UpdateMapPackDisplay();
        
        // Resize container if dynamic resizing is enabled
        if (enableDynamicResizing && isInitialized)
        {
            LogDebug("UpdateUI - Triggering resize");
            ResizeContainer();
        }
        else
        {
            LogDebug($"UpdateUI - Resize skipped - Dynamic: {enableDynamicResizing}, Initialized: {isInitialized}");
        }
    }
    
    /// <summary>
    /// Updates the score display
    /// </summary>
    private void UpdateScoreDisplay()
    {
        if (scoreText != null && GameLogic.Instance != null)
        {
            int totalScore = GameLogic.Instance.GetCurrentScore();
            scoreText.text = string.Format(scoreFormat, totalScore);
        }
    }
    
    /// <summary>
    /// Updates the round display
    /// </summary>
    private void UpdateRoundDisplay()
    {
        if (roundText != null && GameLogic.Instance != null)
        {
            int currentRound = GameLogic.Instance.GetCurrentRound();
            int totalRounds = GameLogic.Instance.GetTotalRounds();
            roundText.text = string.Format(roundFormat, currentRound, totalRounds);
        }
    }
    
    /// <summary>
    /// Updates the map pack display
    /// </summary>
    private void UpdateMapPackDisplay()
    {
        if (mapPackText != null && GameLogic.Instance != null)
        {
            string mapPackName = GameLogic.Instance.GetMapPackName();
            mapPackText.text = string.Format(mapPackFormat, mapPackName);
        }
    }
    
    // Event handlers
    private void OnScoreUpdated(int totalScore)
    {
        UpdateScoreDisplay();
        LogDebug($"Score updated to: {totalScore}");
    }
    
    private void OnRoundUpdated(int currentRound, int totalRounds)
    {
        UpdateRoundDisplay();
        LogDebug($"Round updated to: {currentRound}/{totalRounds}");
    }
    
    private void OnGameEnded(int finalScore)
    {
        UpdateUI();
        LogDebug($"Game ended with final score: {finalScore}");
    }
    
    private void OnMapPackChanged(string mapPackName)
    {
        UpdateMapPackDisplay();
        
        // Trigger resize when MapPack changes (most likely to change width)
        if (enableDynamicResizing && isInitialized)
        {
            ResizeContainer();
        }
        
        LogDebug($"MapPack changed to: {mapPackName}");
    }
    
    // Public methods for external access
    public void SetScoreFormat(string format)
    {
        scoreFormat = format;
        UpdateScoreDisplay();
    }
    
    public void SetRoundFormat(string format)
    {
        roundFormat = format;
        UpdateRoundDisplay();
    }
    
    public void SetMapPackFormat(string format)
    {
        mapPackFormat = format;
        UpdateMapPackDisplay();
    }
    
    /// <summary>
    /// Manually refresh the MapPack display
    /// </summary>
    public void RefreshMapPackDisplay()
    {
        UpdateMapPackDisplay();
        
        // Trigger resize after updating display
        if (enableDynamicResizing && isInitialized)
        {
            ResizeContainer();
        }
    }
    
    /// <summary>
    /// Forces a resize of the container
    /// </summary>
    public void ForceResize()
    {
        if (enableDynamicResizing && isInitialized)
        {
            // Force text layout update before measuring
            ForceTextLayoutUpdate();
            ResizeContainer();
        }
    }
    
    /// <summary>
    /// Forces all text components to update their layout
    /// </summary>
    private void ForceTextLayoutUpdate()
    {
        if (scoreText != null) scoreText.ForceMeshUpdate();
        if (roundText != null) roundText.ForceMeshUpdate();
        if (mapPackText != null) mapPackText.ForceMeshUpdate();
    }
    
    /// <summary>
    /// Checks if the map text is overflowing its container
    /// </summary>
    /// <returns>True if text is overflowing, false otherwise</returns>
    private bool IsMapTextOverflowing()
    {
        if (mapPackText == null || mapTextContainerRect == null) return false;
        
        // Force text layout update
        mapPackText.ForceMeshUpdate();
        
        // Get the preferred width of the text
        float textPreferredWidth = mapPackText.preferredWidth;
        
        // Get the current width of the container
        float containerWidth = mapTextContainerRect.rect.width;
        
        // Check if text width exceeds container width (with small tolerance)
        return textPreferredWidth > (containerWidth - 10f); // 10px tolerance
    }
    
    /// <summary>
    /// Resets the banner to its original size and position
    /// </summary>
    public void ResetToOriginalSize()
    {
        if (containerRect != null && isInitialized)
        {
            // Reset container size (ensure minimum width)
            Vector2 originalSize = containerRect.sizeDelta;
            originalSize.x = Mathf.Max(originalWidth, MINIMUM_WIDTH);
            containerRect.sizeDelta = originalSize;
            
            // Reset container position
            containerRect.anchoredPosition = originalAnchoredPosition;
            
            // Reset map text container to its original size and position
            if (mapTextContainerRect != null)
            {
                Vector2 mapTextSize = mapTextContainerRect.sizeDelta;
                mapTextSize.x = 75f; // Reset to original map text container width
                mapTextContainerRect.sizeDelta = mapTextSize;
                
                // Reset map text container position to original (this should be handled by anchoring, but let's be explicit)
                // The map text container should be anchored to the left edge of the score-banner
                LogDebug("Map text container reset to original width: 75px and position");
            }
            
            // Background will automatically adjust to container size
            // No need to manually reset other child elements
            
            LogDebug("Banner reset to original size and position");
        }
    }
    
    #region Dynamic Resizing
    
    /// <summary>
    /// Initializes the resizing system
    /// </summary>
    private void InitializeResizing()
    {
        // Get container rect if not assigned
        if (containerRect == null)
        {
            containerRect = GetComponent<RectTransform>();
        }
        
        // Find background rect if not assigned
        if (backgroundRect == null)
        {
            // Look for a child object with "background" in the name
            Transform background = transform.Find("Background");
            if (background == null)
            {
                // Look for any child with RectTransform
                for (int i = 0; i < transform.childCount; i++)
                {
                    RectTransform childRect = transform.GetChild(i).GetComponent<RectTransform>();
                    if (childRect != null)
                    {
                        backgroundRect = childRect;
                        break;
                    }
                }
            }
            else
            {
                backgroundRect = background.GetComponent<RectTransform>();
            }
        }
        
        // Find map text container rect if not assigned
        if (mapTextContainerRect == null)
        {
            // Look for a child object that might contain the map text
            // This could be named "MapContainer", "MapTextContainer", or similar
            Transform mapContainer = transform.Find("MapContainer");
            if (mapContainer == null)
            {
                mapContainer = transform.Find("MapTextContainer");
            }
            if (mapContainer == null)
            {
                // Look for any child that contains the mapPackText
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child != null && child.GetComponentInChildren<TextMeshProUGUI>() == mapPackText)
                    {
                        mapContainer = child;
                        break;
                    }
                }
            }
            
            if (mapContainer != null)
            {
                mapTextContainerRect = mapContainer.GetComponent<RectTransform>();
            }
        }
        
        // Store original width and position
        if (containerRect != null)
        {
            trueOriginalWidth = containerRect.sizeDelta.x;
            originalWidth = Mathf.Max(trueOriginalWidth, MINIMUM_WIDTH);
            originalAnchoredPosition = containerRect.anchoredPosition;
            isInitialized = true;
            LogDebug($"Resizing initialized - True original: {trueOriginalWidth}, Enforced: {originalWidth}, Position: {originalAnchoredPosition}");
            LogDebug($"Container current size: {containerRect.sizeDelta}");
        }
        else
        {
            LogError("Container RectTransform not found - dynamic resizing disabled");
        }
    }
    
    /// <summary>
    /// Resizes the container to fit all text content
    /// </summary>
    private void ResizeContainer()
    {
        if (containerRect == null || !isInitialized) 
        {
            LogDebug($"ResizeContainer - Skipped - Container: {containerRect != null}, Initialized: {isInitialized}");
            return;
        }
        
        LogDebug("ResizeContainer - Starting resize process");
        
        // Force text layout update before measuring
        ForceTextLayoutUpdate();
        
        // Check if map text is overflowing
        bool isOverflowing = IsMapTextOverflowing();
        
        if (!isOverflowing)
        {
            return;
        }
        
        // Calculate required width for the map text content
        float mapTextRequiredWidth = CalculateMapTextWidth();
        float currentMapTextWidth = mapTextContainerRect != null ? mapTextContainerRect.rect.width : 0f;
        
        // Calculate required score-banner width
        float mapTextWidthDifference = mapTextRequiredWidth - currentMapTextWidth;
        float requiredWidth = originalWidth + mapTextWidthDifference;
        requiredWidth = Mathf.Max(requiredWidth, MINIMUM_WIDTH);
        float widthDifference = requiredWidth - originalWidth;
        
        if (Mathf.Abs(widthDifference) > 1f)
        {
            // Update container width
            Vector2 newSize = containerRect.sizeDelta;
            newSize.x = requiredWidth;
            containerRect.sizeDelta = newSize;
            
            // Move the score-banner left by half of widthDifference for balanced expansion
            Vector2 newPosition = containerRect.anchoredPosition;
            newPosition.x = originalAnchoredPosition.x - (widthDifference * 0.5f);
            containerRect.anchoredPosition = newPosition;
            
            // Expand the map text container by the map text difference
            if (mapTextContainerRect != null && mapTextWidthDifference > 0f)
            {
                Vector2 mapTextSize = mapTextContainerRect.sizeDelta;
                mapTextSize.x += mapTextWidthDifference;
                mapTextContainerRect.sizeDelta = mapTextSize;
            }

            // Update original width for next calculation
            originalWidth = requiredWidth;
            LogDebug($"Score-banner resized: width {requiredWidth}px, moved left {widthDifference * 0.5f}px");
        }
        
        // Map text container will automatically stretch to fill parent container
        // No need to manually resize it - it's anchored to left edge and will stretch
    }
    
    /// <summary>
    /// Calculates the required width for the map text container only
    /// </summary>
    /// <returns>Required width in pixels</returns>
    private float CalculateMapTextWidth()
    {
        // Calculate dynamic padding based on container size
        CalculateDynamicValues();
        
        float totalWidth = dynamicPadding * 2; // Left and right padding
        
        // Only measure map pack text (left-anchored content)
        if (mapPackText != null && !string.IsNullOrEmpty(mapPackText.text))
        {
            float textWidth = GetTextWidth(mapPackText);
            totalWidth += textWidth;
            LogDebug($"MapPack text width: {textWidth}");
        }
        
        LogDebug($"Map text container calculated width: {totalWidth}");
        
        return totalWidth;
    }
    
    /// <summary>
    /// Calculates dynamic sizing values based on container and text properties
    /// </summary>
    private void CalculateDynamicValues()
    {
        if (containerRect == null) return;
        
        // Calculate padding as a percentage of container height (for proportional spacing)
        float containerHeight = containerRect.sizeDelta.y;
        dynamicPadding = Mathf.Max(10f, containerHeight * 0.1f); // 10% of height, minimum 10px
        
        // Calculate safety margin based on text size
        if (mapPackText != null)
        {
            float fontSize = mapPackText.fontSize;
            dynamicSafetyMargin = Mathf.Max(10f, fontSize * 0.5f); // Half font size, minimum 10px
        }
        else
        {
            dynamicSafetyMargin = 20f; // Default fallback
        }
        
        LogDebug($"Dynamic values - Padding: {dynamicPadding}, Safety margin: {dynamicSafetyMargin}");
    }
    
    /// <summary>
    /// Calculates the required width to fit all text content (legacy method)
    /// </summary>
    /// <returns>Required width in pixels</returns>
    private float CalculateRequiredWidth()
    {
        // Calculate dynamic padding
        CalculateDynamicValues();
        
        float totalWidth = dynamicPadding * 2; // Left and right padding
        int textElementCount = 0;
        
        // Measure each text element
        if (scoreText != null && !string.IsNullOrEmpty(scoreText.text))
        {
            float textWidth = GetTextWidth(scoreText);
            totalWidth += textWidth + 10f; // Fixed small spacing
            textElementCount++;
            LogDebug($"Score text width: {textWidth}");
        }
        
        if (roundText != null && !string.IsNullOrEmpty(roundText.text))
        {
            float textWidth = GetTextWidth(roundText);
            totalWidth += textWidth + 10f; // Fixed small spacing
            textElementCount++;
            LogDebug($"Round text width: {textWidth}");
        }
        
        if (mapPackText != null && !string.IsNullOrEmpty(mapPackText.text))
        {
            float textWidth = GetTextWidth(mapPackText);
            totalWidth += textWidth + 10f; // Fixed small spacing
            textElementCount++;
            LogDebug($"MapPack text width: {textWidth}");
        }
        
        // Remove last spacing if we have any text elements
        if (textElementCount > 0)
        {
            totalWidth -= 10f; // Remove last spacing
        }
        
        LogDebug($"Total calculated width: {totalWidth} (elements: {textElementCount})");
        
        return totalWidth;
    }
    
    /// <summary>
    /// Gets the width of a TextMeshProUGUI text element
    /// </summary>
    /// <param name="textComponent">The text component to measure</param>
    /// <returns>Width in pixels</returns>
    private float GetTextWidth(TextMeshProUGUI textComponent)
    {
        if (textComponent == null) return 0f;
        
        // Force text to update its layout
        textComponent.ForceMeshUpdate();
        
        // Get the preferred width
        float preferredWidth = textComponent.preferredWidth;
        
        // Add dynamic safety margin to prevent overflow
        float widthWithMargin = preferredWidth + dynamicSafetyMargin;
        
        LogDebug($"Text '{textComponent.text}' - Preferred: {preferredWidth}, With margin: {widthWithMargin}");
        
        return widthWithMargin;
    }
    
    /// <summary>
    /// Determines if a RectTransform is right-anchored
    /// </summary>
    /// <param name="rectTransform">The RectTransform to check</param>
    /// <returns>True if right-anchored, false otherwise</returns>
    private bool IsRightAnchored(RectTransform rectTransform)
    {
        if (rectTransform == null) return false;
        
        // Check if the anchor is on the right side (x > 0.5)
        Vector2 anchorMin = rectTransform.anchorMin;
        Vector2 anchorMax = rectTransform.anchorMax;
        
        // Right-anchored if both min and max x are > 0.5
        bool isRightAnchored = anchorMin.x > 0.5f && anchorMax.x > 0.5f;
        
        // Also check if it's center-right anchored (min < 0.5 but max > 0.5)
        bool isCenterRightAnchored = anchorMin.x < 0.5f && anchorMax.x > 0.5f;
        
        return isRightAnchored || isCenterRightAnchored;
    }
    
    /// <summary>
    /// Determines if the background is anchored to the container
    /// </summary>
    /// <returns>True if background is anchored to container, false if anchored to canvas</returns>
    private bool IsBackgroundAnchoredToContainer()
    {
        if (backgroundRect == null || containerRect == null) return false;
        
        // Check if the background's parent is the container
        return backgroundRect.parent == containerRect;
    }
    
    /// <summary>
    /// Enables or disables dynamic resizing
    /// </summary>
    /// <param name="enabled">Whether to enable resizing</param>
    public void SetDynamicResizing(bool enabled)
    {
        enableDynamicResizing = enabled;
        if (enabled && isInitialized)
        {
            ResizeContainer();
        }
    }
    
    
    #endregion
    
    // Debug logging methods
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ScoreBannerController] {message}");
        }
    }
    
    private void LogWarning(string message)
    {
            Debug.LogWarning($"[ScoreBannerController] {message}");
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[ScoreBannerController] {message}");
    }
}
