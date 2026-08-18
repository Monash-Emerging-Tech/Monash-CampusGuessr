
#nullable enable
using UnityEngine;
using System;


/// <summary>
/// Manages interactions between Unity and the MazeMap JavaScript API
/// Handles map clicks, scoring, and marker placement
/// 
/// Written by aleu0007
/// Last Modified: 29/01/2026
/// </summary>
public class MapInteractionManager : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private bool enableMapOnStart = false;
    [SerializeField] private float maxGuessDistance = 1000f; // Maximum distance for scoring in meters

    [Header("Z-Level Settings")]
    [SerializeField] private int minZLevel = -4; // P4 (Parking Level 4)
    [SerializeField] private int maxZLevel = 12; // 11th Floor
    [SerializeField] private int currentZLevel = 0; // Ground level

    [Header("Scoring Settings")]
    [SerializeField] private AnimationCurve scoreCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    private float currentZLevelWeight = 0.5f; // Set per map pack
    private float currentDistanceScale = 1f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Current game state
    private LocationData? currentActualLocation;
    private LocationData? currentGuessLocation;
    private MarkerData? currentGuessMarker;
    private MarkerData? currentActualMarker;
    private bool isMapActive = false;
    // Events
    public static event Action<LocationData>? OnGuessSubmitted; // Event with location data
    public static event Action<int, int, int, bool>? OnScoreCalculated;
    public static event Action? OnMapOpened;
    public static event Action? OnMapClosed;
    public static event Action<int>? OnZLevelChanged; // New z-level event

    // Singleton pattern
    public static MapInteractionManager Instance { get; private set; } = null!;

    #region Unity Lifecycle & Initialization

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (enableMapOnStart)
        {
            HideMap();
        }

        LogDebug("MapInteractionManager initialized");
    }

    #endregion

    #region Map Control

    /// <summary>
    /// Shows the map interface
    /// </summary>
    public void ShowMap()
    {
        if (isMapActive) return;

        isMapActive = true;

        WebMapBridge.ShowMap(enableDebugLogs);

        OnMapOpened?.Invoke();
        LogDebug("Map opened");
    }

    /// <summary>
    /// Hides the map interface
    /// </summary>
    public void HideMap()
    {
        if (!isMapActive) return;

        isMapActive = false;

        WebMapBridge.HideMap(enableDebugLogs);

        OnMapClosed?.Invoke();
        LogDebug("Map closed");
    }

    #endregion

    #region Location Management

    /// <summary>
    /// Sets the actual location for the current round with z-level support
    /// </summary>
    /// <param name="latitude">Latitude of actual location</param>
    /// <param name="longitude">Longitude of actual location</param>
    /// <param name="zLevel">Z-level of actual location</param>
    public void SetActualLocation(float latitude, float longitude, float zLevel)
    {
        currentActualLocation = new LocationData
        {
            latitude = latitude,
            longitude = longitude,
            zLevel = Mathf.RoundToInt(zLevel),
            zLevelName = ZLevelFormatter.GetName(Mathf.RoundToInt(zLevel))
        };

        // Create enhanced marker data
        currentActualMarker = new MarkerData
        {
            lat = latitude,
            lng = longitude,
            zLevel = Mathf.RoundToInt(zLevel),
            zLevelName = ZLevelFormatter.GetName(Mathf.RoundToInt(zLevel))
        };

        LogDebug($"Actual location set to: Latitude:{latitude}, Longitude:{longitude}, zLevel:{zLevel}, zLevelName:{ZLevelFormatter.GetName(Mathf.RoundToInt(zLevel))}");
    }

    /// <summary>
    /// Resets the current round data
    /// </summary>
    public void ResetRound()
    {
        LogDebug($"ResetRound called on instance: {this.GetInstanceID()}");
        currentActualLocation = null;
        currentGuessLocation = null;
        LogDebug("Round reset");
    }

    #endregion

    #region JavaScript Communication

    /// <summary>
    /// Called from JavaScript when map is clicked (enhanced version with z-level)
    /// </summary>
    /// <param name="jsonData">JSON string containing enhanced click data</param>
    public void OnMapClick(string jsonData)
    {
        try
        {
            if (GuessPayloadParser.TryParseMapClick(jsonData, currentZLevel, out var parsedGuess))
            {
                currentGuessLocation = parsedGuess.Location;
                if (parsedGuess.HasMarker)
                {
                    currentGuessMarker = parsedGuess.Marker;
                }
                LogDebug(parsedGuess.LogMessage);
            }
            else
            {
                LogWarning("Could not parse map click data from JSON");
            }
        }
        catch (Exception e)
        {
            LogError($"Error parsing map click data: {e.Message}");
        }
    }

    /// <summary>
    /// Called from JavaScript when guess is submitted
    /// Receives LocationPayload and calculates score
    /// </summary>
    /// <param name="jsonData">JSON string containing guess location data: latitude, longitude, zLevel, zLevelName</param>
    public void SubmitGuess(string jsonData)
    {
        try
        {
            LogDebug($"SubmitGuess called on instance: {this.GetInstanceID()}, currentActualLocation is null: {currentActualLocation == null}");

            if (GuessPayloadParser.TryParseSubmittedGuess(jsonData, currentZLevel, out var parsedGuess))
            {
                currentGuessLocation = parsedGuess.Location;
                if (parsedGuess.HasMarker)
                {
                    currentGuessMarker = parsedGuess.Marker;
                }
                LogDebug(parsedGuess.LogMessage);
            }
            else
            {
                LogWarning("Could not parse guess data from JSON");
            }

            // Trigger guess submitted event
            if (currentGuessLocation != null)
            {
                OnGuessSubmitted?.Invoke(currentGuessLocation);
            }

            // Calculate score if we have both locations
            if (currentActualLocation != null && currentGuessLocation != null)
            {
                var (score, distance, floorDiff, tooHigh) = CalculateScore(currentActualLocation, currentGuessLocation);
                OnScoreCalculated?.Invoke(score, distance, floorDiff, tooHigh);

                // Show both locations on map
                ShowBothLocations();
            }
            else
            {
                LogWarning("Cannot calculate score: guess or actual location missing");
            }
        }
        catch (Exception e)
        {
            LogError($"Error processing guess submission: {e.Message}");
        }
    }

    /// <summary>
    /// Sends actual location data to JavaScript with latitude, longitude, zLevel, zLevelName
    /// </summary>
    /// <param name="latitude">Latitude (x coordinate)</param>
    /// <param name="longitude">Longitude (y coordinate)</param>
    /// <param name="zLevel">Z-level (z coordinate)</param>
    public void SendActualLocationToJavaScript(float latitude, float longitude, int zLevel)
    {
        // Create payload data structure (same format as receiving)
        var locationPayload = new LocationPayload
        {
            latitude = latitude,
            longitude = longitude,
            zLevel = zLevel,
            zLevelName = ZLevelFormatter.GetName(zLevel)
        };

        // Serialize to JSON
        string jsonPayload = JsonUtility.ToJson(locationPayload);

        WebMapBridge.AddActualLocation(jsonPayload, latitude, longitude, zLevel, enableDebugLogs);
    }

    /// <summary>
    /// Updates the web UI guessing state (enables/disables marker placement and controls)
    /// </summary>
    public void SetWebGuessingState(bool isGuessing)
    {
        WebMapBridge.SetGuessingState(isGuessing, enableDebugLogs);
    }

    /// <summary>
    /// Updates the web minimap widget size (e.g. "mm-size-s", "mm-size-m", "mm-size-l")
    /// </summary>
    public void SetWebMapSize(string size)
    {
        WebMapBridge.SetMapSize(size, enableDebugLogs);
    }

    /// <summary>
    /// Clears markers and lines on the web map UI
    /// </summary>
    public void ClearWebMapState()
    {
        WebMapBridge.ClearMapState(enableDebugLogs);
    }
    
    /// <summary>
    /// Sets the map center, zoom level, and campus (triggers MazeMap reinit if campus changes).
    /// </summary>
    public void SetMapCenter(float lat, float lng, int zoom = 16, int campusId = 159)
    {
        WebMapBridge.SetMapCenter(lat, lng, zoom, campusId, enableDebugLogs);
    }
    #endregion

    #region Scoring System

    /// <summary>
    /// Calculates score based on distance between guess and actual location
    /// </summary>
    /// <param name="actual">Actual location data</param>
    /// <param name="guess">Guess location data</param>
    /// <returns>Score from 0 to maxScore</returns>
    private (int score, int distance, int floorDiff, bool tooHigh) CalculateScore(LocationData actual, LocationData guess)
    {
        // New Scoring Method (considers Z-levels AND distance scale) 18/05/2026
        float distance = DistanceCalculator.CalculateMeters(actual, guess);
        int score = ScoreDataScriptableObject.CalculateScore((int)(distance * currentDistanceScale));

        // Apply z-level penalty
        int zLevelDiff = Mathf.Abs(actual.zLevel - guess.zLevel);
        if (zLevelDiff > 0)
        {
            float zPenalty = Mathf.Min(zLevelDiff * 0.25f * currentZLevelWeight, 1f);
            float zModifier = 1f - zPenalty;
            int preZScore = score;
            score = Mathf.RoundToInt(score * zModifier);
            LogDebug($"Z-level penalty applied: diff={zLevelDiff}, weight={currentZLevelWeight}, modifier={zModifier:F2}, score {preZScore} -> {score}");
        }

        LogDebug($"Distance: {distance:F2}m, Score: {score}");
        bool tooHigh = guess.zLevel > actual.zLevel;
        return (score, (int)distance, zLevelDiff, tooHigh);;
    }

    /// <summary>
    /// Sets the z-level weight for score penalty calculation (called when map pack changes)
    /// </summary>
    public void SetZLevelWeight(float weight)
    {
        currentZLevelWeight = Mathf.Clamp01(weight);
        LogDebug($"Z-level weight set to: {currentZLevelWeight}");
    }

    public void SetDistanceScale(float scale)
    {
        currentDistanceScale = scale;
        LogDebug($"Distance scale set to: {currentDistanceScale}");
    }

    #endregion

    #region UI Communication

    /// <summary>
    /// Updates the score display in the web interface
    /// </summary>
    /// <param name="score">Current score</param>
    /// <param name="round">Current round</param>
    public void UpdateScoreDisplay(int score, int round)
    {
        WebMapBridge.UpdateScoreDisplay(score, round, enableDebugLogs);
    }

    /// <summary>
    /// Shows loading indicator
    /// </summary>
    /// <param name="show">Whether to show or hide loading</param>
    public void ShowLoading(bool show)
    {
        WebMapBridge.ShowLoading(show, enableDebugLogs);
    }

    #endregion

    #region Map Markers

    /// <summary>
    /// Shows both actual and guess locations on the map
    /// </summary>
    public void ShowBothLocations()
    {
        if (currentActualLocation == null || currentGuessLocation == null) return;

        WebMapBridge.AddMarker(currentActualLocation.latitude, currentActualLocation.longitude, "Actual Location", "actual", enableDebugLogs);
        WebMapBridge.AddMarker(currentGuessLocation.latitude, currentGuessLocation.longitude, "Your Guess", "guess", enableDebugLogs);

        LogDebug("Both locations displayed on map");
    }

    #endregion

    #region Debug Logging

    // Debug logging methods
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[MapInteractionManager] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[MapInteractionManager] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[MapInteractionManager] {message}");
    }

    #endregion

    #region Z-Level Management

    /// <summary>
    /// Validates if a z-level is within allowed range
    /// </summary>
    /// <param name="zLevel">Z-level to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValidZLevel(int zLevel)
    {
        return zLevel >= minZLevel && zLevel <= maxZLevel;
    }

    /// <summary>
    /// Sets the current z-level
    /// </summary>
    /// <param name="zLevel">New z-level</param>
    public void SetZLevel(int zLevel)
    {
        if (IsValidZLevel(zLevel))
        {
            currentZLevel = zLevel;
            OnZLevelChanged?.Invoke(zLevel);
            LogDebug($"Z-level changed to: {ZLevelFormatter.GetName(zLevel)}");
        }
        else
        {
            LogWarning($"Invalid z-level: {zLevel}. Must be between {minZLevel} and {maxZLevel}");
        }
    }

    /// <summary>
    /// Gets the current z-level
    /// </summary>
    /// <returns>Current z-level</returns>
    public int GetCurrentZLevel()
    {
        return currentZLevel;
    }


    #endregion

    #region Data Structures

    // Custom marker options for visual customization
    [System.Serializable]
    public class MarkerOptions
    {
        public string imgUrl = "";
        public float imgScale = 1.7f;
        public string color = "white";
        public int size = 60;
        public bool innerCircle = false;
        public string shape = "marker";
        public int zLevel = 0;
    }

    // Complete marker data structure
    [System.Serializable]
    public class MarkerData
    {
        public string? id;
        public float lng;
        public float lat;
        public int zLevel;
        public string? zLevelName;
        public string? timestamp;
        public MarkerOptions? options;
        public string? markerType; // "player" or "actual"
    }

    // Data structure for sending/receiving location payload data (same format for both directions)
    [System.Serializable]
    public class LocationPayload
    {
        public float latitude;
        public float longitude;
        public int zLevel;
        public string? zLevelName;
    }

    // Location data structure with lat, lng, zLevel, zLevelName
    [System.Serializable]
    public class LocationData
    {
        public float latitude;
        public float longitude;
        public int zLevel;
        public string? zLevelName;
    }

    #endregion
}
