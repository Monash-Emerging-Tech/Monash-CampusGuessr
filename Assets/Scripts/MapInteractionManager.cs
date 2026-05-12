
#nullable enable
using UnityEngine;
using System.Collections.Generic;
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
    // WebGL JavaScript interop
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void showMapFromUnity();
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void hideMapFromUnity();
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void addActualLocationFromUnity(string json);
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void setGuessingStateFromUnity(bool isGuessing);
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void mmSetWidgetSize(string size);
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void clearMapStateFromUnity();
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void updateScoreFromUnity(int score, int round);
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void showLoading(bool show);
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void addMarkerFromUnity(float lat, float lng, string label, string type);
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void setMapPackViewFromUnity(int campusId, float lat, float lng, int zoom);
#endif
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
    public static event Action<int, int>? OnScoreCalculated;
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

        // Call JavaScript function to show map

#if UNITY_WEBGL && !UNITY_EDITOR
    showMapFromUnity();
#else
        LogDebug("Map would be shown (WebGL only)");
#endif

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

        // Call JavaScript function to hide map
#if UNITY_WEBGL && !UNITY_EDITOR
    hideMapFromUnity();
#else
        LogDebug("Map would be hidden (WebGL only)");
#endif

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
            zLevelName = GetZLevelName(Mathf.RoundToInt(zLevel))
        };

        // Create enhanced marker data
        currentActualMarker = new MarkerData
        {
            lat = latitude,
            lng = longitude,
            zLevel = Mathf.RoundToInt(zLevel),
            zLevelName = GetZLevelName(Mathf.RoundToInt(zLevel))
        };

        LogDebug($"Actual location set to: Latitude:{latitude}, Longitude:{longitude}, zLevel:{zLevel}, zLevelName:{GetZLevelName(Mathf.RoundToInt(zLevel))}");
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
            // Try to parse as enhanced data first
            var enhancedData = JsonUtility.FromJson<EnhancedMapClickData>(jsonData);
            if (enhancedData != null && !string.IsNullOrEmpty(enhancedData.zLevelName))
            {
                // Enhanced data with z-level

                currentGuessLocation = new LocationData
                {
                    latitude = enhancedData.latitude,
                    longitude = enhancedData.longitude,
                    zLevel = enhancedData.zLevel,
                    zLevelName = enhancedData.zLevelName
                };

                // Create enhanced marker data
                currentGuessMarker = new MarkerData
                {
                    id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                    lng = enhancedData.longitude,
                    lat = enhancedData.latitude,
                    zLevel = enhancedData.zLevel,
                    zLevelName = enhancedData.zLevelName,
                    timestamp = enhancedData.timestamp.ToString(),
                    options = new MarkerOptions
                    {
                        imgUrl = "images/handthing.svg",
                        imgScale = 1.7f,
                        color = "white",
                        size = 60,
                        innerCircle = false,
                        shape = "marker",
                        zLevel = enhancedData.zLevel
                    },
                    markerType = "player"
                };

                LogDebug($"Map clicked at: {enhancedData.latitude}, {enhancedData.longitude}, Level: {enhancedData.zLevelName}");
            }
            else
            {
                // Fallback to legacy data
                var clickData = JsonUtility.FromJson<MapClickData>(jsonData);
                currentGuessLocation = new LocationData
                {
                    latitude = clickData.latitude,
                    longitude = clickData.longitude,
                    zLevel = currentZLevel,
                    zLevelName = GetZLevelName(currentZLevel)
                };
                LogDebug($"Map clicked at: {clickData.latitude}, {clickData.longitude} (legacy data)");
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

            // Try to parse enhanced payload first
            var payload = JsonUtility.FromJson<LocationPayload>(jsonData);
            if (payload != null && !string.IsNullOrEmpty(payload.zLevelName))
            {
                // Enhanced data with z-level
                currentGuessLocation = new LocationData
                {
                    latitude = payload.latitude,
                    longitude = payload.longitude,
                    zLevel = payload.zLevel,
                    zLevelName = payload.zLevelName
                };

                // Create enhanced marker data for guess
                currentGuessMarker = new MarkerData
                {
                    id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                    lng = payload.longitude,
                    lat = payload.latitude,
                    zLevel = payload.zLevel,
                    zLevelName = payload.zLevelName,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                    options = new MarkerOptions
                    {
                        imgUrl = "images/handthing.svg",
                        imgScale = 1.7f,
                        color = "white",
                        size = 60,
                        innerCircle = false,
                        shape = "marker",
                        zLevel = payload.zLevel
                    },
                    markerType = "player"
                };

                LogDebug($"Guess submitted at: latitude:{payload.latitude}, longitude:{payload.longitude}, zLevel: {payload.zLevelName}");
            }
            else
            {
                // Fallback to legacy data
                var guessData = JsonUtility.FromJson<MapClickData>(jsonData);
                if (guessData != null)
                {
                    currentGuessLocation = new LocationData
                    {
                        latitude = guessData.latitude,
                        longitude = guessData.longitude,
                        zLevel = currentZLevel,
                        zLevelName = GetZLevelName(currentZLevel)
                    };

                    // Create basic marker data for legacy guess
                    currentGuessMarker = new MarkerData
                    {
                        id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                        lng = guessData.longitude,
                        lat = guessData.latitude,
                        zLevel = currentZLevel, // Use current z-level
                        zLevelName = GetZLevelName(currentZLevel),
                        timestamp = guessData.timestamp.ToString(),
                        options = new MarkerOptions
                        {
                            imgUrl = "images/handthing.svg",
                            imgScale = 1.7f,
                            color = "white",
                            size = 60,
                            innerCircle = false,
                            shape = "marker",
                            zLevel = currentZLevel
                        },
                        markerType = "player"
                    };

                    LogDebug($"Guess submitted at: {guessData.latitude}, {guessData.longitude} (legacy data)");
                }
                else
                {
                    LogWarning("Could not parse guess data from JSON");
                }
            }

            // Trigger guess submitted event
            if (currentGuessLocation != null)
            {
                OnGuessSubmitted?.Invoke(currentGuessLocation);
            }

            // Calculate score if we have both locations
            if (currentActualLocation != null && currentGuessLocation != null)
            {
                var (score, distance) = CalculateScore(currentActualLocation, currentGuessLocation);
                OnScoreCalculated?.Invoke(score, distance);

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
            zLevelName = GetZLevelName(zLevel)
        };

        // Serialize to JSON
        string jsonPayload = JsonUtility.ToJson(locationPayload);

#if UNITY_WEBGL && !UNITY_EDITOR
    addActualLocationFromUnity(jsonPayload);
#else
        LogDebug($"Actual location would be sent to JavaScript: ({latitude}, {longitude}), Level: {GetZLevelName(zLevel)}");
#endif
    }

    /// <summary>
    /// Updates the web UI guessing state (enables/disables marker placement and controls)
    /// </summary>
    public void SetWebGuessingState(bool isGuessing)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    setGuessingStateFromUnity(isGuessing);
#else
        LogDebug($"Guessing state would be sent to JavaScript: {(isGuessing ? "Guessing" : "Results")}");
#endif
    }

    /// <summary>
    /// Updates the web minimap widget size (e.g. "mm-size-s", "mm-size-m", "mm-size-l")
    /// </summary>
    public void SetWebMapSize(string size)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    mmSetWidgetSize(size);
#else
        LogDebug($"Map size would be sent to JavaScript: {size}");
#endif
    }

    /// <summary>
    /// Clears markers and lines on the web map UI
    /// </summary>
    public void ClearWebMapState()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    clearMapStateFromUnity();
#else
        LogDebug("Map state would be cleared on JavaScript");
#endif
    }
    
/// <summary>
/// Sets the map center, zoom level, and campus (triggers MazeMap reinit if campus changes).
/// </summary>
public void SetMapCenter(float lat, float lng, int zoom = 16, int campusId = 159)
{
#if UNITY_WEBGL && !UNITY_EDITOR
    setMapPackViewFromUnity(campusId, lat, lng, zoom);
#else
    LogDebug($"Map center would be set to: {lat}, {lng}, zoom: {zoom}, campusId: {campusId}");
#endif
}
    #endregion

    #region Scoring System

    /// <summary>
    /// Calculates score based on distance between guess and actual location
    /// </summary>
    /// <param name="actual">Actual location data</param>
    /// <param name="guess">Guess location data</param>
    /// <returns>Score from 0 to maxScore</returns>
    private (int score, int distance) CalculateScore(LocationData actual, LocationData guess)
    {
        // New Scoring Method (considers Z-levels) 13/05/2026
        float distance = CalculateDistance(actual, guess);
        int score = ScoreDataScriptableObject.CalculateScore((int)distance);

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
        return (score, (int)distance);
    }

    /// <summary>
    /// Calculates distance between two coordinates using simple approximation
    /// Accurate enough for campus distances (under 5km)
    /// </summary>
    /// <param name="coord1">First location data</param>
    /// <param name="coord2">Second location data</param>
    /// <returns>Distance in meters</returns>
    private float CalculateDistance(LocationData coord1, LocationData coord2)
    {
        // Convert degrees to approximate meters
        // 1 degree latitude ~ 111,000 meters
        // 1 degree longitude ~ 111,000 * cos(latitude) meters
        float latDiff = (coord2.latitude - coord1.latitude) * 111000f;
        float lngDiff = (coord2.longitude - coord1.longitude) * 111000f * Mathf.Cos(coord1.latitude * Mathf.Deg2Rad);

        return Mathf.Sqrt(latDiff * latDiff + lngDiff * lngDiff);
    }

    /// <summary>
    /// Sets the z-level weight for score penalty calculation (called when map pack changes)
    /// </summary>
    public void SetZLevelWeight(float weight)
    {
        currentZLevelWeight = Mathf.Clamp01(weight);
        LogDebug($"Z-level weight set to: {currentZLevelWeight}");
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
#if UNITY_WEBGL && !UNITY_EDITOR
    updateScoreFromUnity(score, round);
#else
        LogDebug($"Score would be updated: {score}, Round: {round}");
#endif
    }

    /// <summary>
    /// Shows loading indicator
    /// </summary>
    /// <param name="show">Whether to show or hide loading</param>
    public void ShowLoading(bool show)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    showLoading(show);
#else
        LogDebug($"Loading would be {(show ? "shown" : "hidden")}");
#endif
    }

    #endregion

    #region Map Markers

    /// <summary>
    /// Shows both actual and guess locations on the map
    /// </summary>
    public void ShowBothLocations()
    {
        if (currentActualLocation == null || currentGuessLocation == null) return;


        // Add actual location marker
#if UNITY_WEBGL && !UNITY_EDITOR
    addMarkerFromUnity(currentActualLocation.latitude, currentActualLocation.longitude, "Actual Location", "actual");
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    addMarkerFromUnity(currentGuessLocation.latitude, currentGuessLocation.longitude, "Your Guess", "guess");
#endif

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
    /// Converts z-level number to readable name
    /// </summary>
    /// <param name="zLevel">Z-level number</param>
    /// <returns>Readable level name</returns>
    private string GetZLevelName(int zLevel)
    {
        if (zLevel == -4) return "P4 (Parking Level 4)";
        if (zLevel == -3) return "P3 (Parking Level 3)";
        if (zLevel == -2) return "P2 (Parking Level 2)";
        if (zLevel == -1) return "P1 (Parking Level 1)";
        if (zLevel == 0) return "LG (Lower Ground)";
        if (zLevel == 1) return "G (Ground)";
        if (zLevel == 2) return "1 (First Floor)";
        if (zLevel == 3) return "2 (Second Floor)";
        if (zLevel == 4) return "3 (Third Floor)";
        if (zLevel == 5) return "4 (Fourth Floor)";
        if (zLevel == 6) return "5 (Fifth Floor)";
        if (zLevel == 7) return "6 (Sixth Floor)";
        if (zLevel == 8) return "7 (Seventh Floor)";
        if (zLevel == 9) return "8 (Eighth Floor)";
        if (zLevel == 10) return "9 (Ninth Floor)";
        if (zLevel == 11) return "10 (Tenth Floor)";
        if (zLevel == 12) return "11 (Eleventh Floor)";
        if (zLevel < -4) return $"B{Math.Abs(zLevel)} (Basement {Math.Abs(zLevel)})";
        return $"{zLevel} (Level {zLevel})";
    }

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
            LogDebug($"Z-level changed to: {GetZLevelName(zLevel)}");
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

    // Enhanced data structure for JSON parsing with z-level support
    [System.Serializable]
    public class EnhancedMapClickData
    {
        public float latitude;
        public float longitude;
        public int zLevel;
        public string? zLevelName;
        public long timestamp;
    }

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

    // Legacy data structure for backward compatibility
    [System.Serializable]
    private class MapClickData
    {
        public float latitude;
        public float longitude;
        public long timestamp;
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