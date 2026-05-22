using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Controls the breakdown display showing all previous rounds with location names and scores.
/// Creates simple text elements for each round in the game breakdown.
/// 
/// Written by madyyson
/// Last Modified: 12/02/2026
/// </summary>
public class BreakdownController : MonoBehaviour
{
    [Header("Data References")]
    [SerializeField] private ScoreDataScriptableObject scoreData;
    
    [Header("UI References")]
    [SerializeField] private Transform parentContainer; // Parent for spawned breakdown items
    [SerializeField] private TMP_FontAsset font; // Optional: assign a font asset
    
    [Header("Text Settings")]
    [SerializeField] private float fontSize = 36f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private TextAlignmentOptions alignment = TextAlignmentOptions.Left;
    [SerializeField] private Vector2 itemSize = new Vector2(800f, 60f); // Width and height of each text item
    
    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // List to keep track of spawned breakdown items
    private List<GameObject> spawnedBreakdownItems = new List<GameObject>();
    
    private void Start()
    {
        DisplayBreakdown();
    }
    
    /// <summary>
    /// Main method to display the breakdown of all previous rounds
    /// </summary>
    public void DisplayBreakdown()
    {
        LogDebug("Displaying breakdown...");
        
        // Clear any existing breakdown items
        ClearBreakdown();
        
        // Validate required components
        if (scoreData == null)
        {
            LogError("ScoreData reference is null! Cannot display breakdown.");
            return;
        }
        
        if (parentContainer == null)
        {
            LogError("Parent container is null! Cannot spawn breakdown items.");
            return;
        }
        
        // Get data from ScoreData
        var locations = scoreData.PreviousLocations;
        var scores = scoreData.PreviousScores;
        
        int roundCount = Mathf.Min(locations.Count, scores.Count);
        
        LogDebug($"Found {roundCount} rounds to display");
        
        if (roundCount == 0)
        {
            // Show a message if no rounds played
            CreateTextItem("No rounds played yet.");
            return;
        }
        
        // Create a text item for each round
        for (int i = 0; i < roundCount; i++)
        {
            int roundNumber = i + 1;
            string locationName = locations[i].Name;
            int score = scores[i];
            
            string displayText = $"Round {roundNumber}: {locationName} -> {score} pts";
            CreateTextItem(displayText);
        }
    }
    
    /// <summary>
    /// Creates a single text item with the given text
    /// </summary>
    private void CreateTextItem(string text)
    {
        // Create a new GameObject for the text
        GameObject textObj = new GameObject($"BreakdownText_{spawnedBreakdownItems.Count}");
        textObj.transform.SetParent(parentContainer, false);
        
        // Add RectTransform
        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = itemSize;
        
        // Add TextMeshProUGUI component
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = textColor;
        textComponent.alignment = alignment;
        
        // Apply font if assigned
        if (font != null)
        {
            textComponent.font = font;
        }
        
        // Add to tracking list
        spawnedBreakdownItems.Add(textObj);
        
        LogDebug($"Created text item: {text}");
    }
    
    /// <summary>
    /// Clears all spawned breakdown items
    /// </summary>
    public void ClearBreakdown()
    {
        LogDebug("Clearing breakdown items...");
        
        foreach (GameObject item in spawnedBreakdownItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        
        spawnedBreakdownItems.Clear();
    }
    
    /// <summary>
    /// Refresh the breakdown display (useful if data changes)
    /// </summary>
    public void RefreshBreakdown()
    {
        LogDebug("Refreshing breakdown display...");
        DisplayBreakdown();
    }
    
    #region Debug Logging
    
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[BreakdownController] {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[BreakdownController] {message}");
    }
    
    #endregion
}