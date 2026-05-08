using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>
/// GameLogic, Responsible for all game logic including scoring, round management, and game state.
/// Communicates with MapInteractionManager for map interactions.
/// 
/// Written by O-Bolt
/// Modified by aleu0007
/// Last Modified: 1/02/2026
/// </summary>
public class GameLogic : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int totalRounds = 5;
    // [SerializeField] private int gameMode = 0; // TODO
    [SerializeField] private string mapPackName = "all";
    [SerializeField] private ScoreDataScriptableObject scoreData; // Scriptable Object to hold score data

    [Header("Map Integration")]
    [SerializeField] private MapInteractionManager mapManager;
    [SerializeField] private LocationManager locationManager;

    [Header("UI References")]
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject mapUI;
    [SerializeField] private GameObject resultsUI;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;


    // Game state
    private int currentScore = 0;
    private int currentRound = 1;
    private bool inGame = false;
    private bool isGuessing = false;
    private bool isGameActive = false;
    private bool isRoundActive = false;
    private bool skipNextRoundWait = false;

    // MapPack management
    private int resolvedMapPackId = 2; // Default to Monash 101
    private float pendingMapCenterLat = -37.9106f;
    private float pendingMapCenterLng = 145.1361f;
    private int pendingMapZoom = 16;
    private int pendingCampusId = 159;

    private Dictionary<int, LocationManager.MapPack> allMapPacks;

    // Events
    public static event System.Action<int> OnRoundStarted;
    public static event System.Action<int> OnRoundEnded;
    public static event System.Action<int> OnGameEnded;
    public static event System.Action<int> OnScoreUpdated;
    public static event System.Action<int, int> OnRoundUpdated; // currentRound, totalRounds
    public static event System.Action<string> OnMapPackChanged; // mapPackName

    // Singleton pattern
    public static GameLogic Instance { get; private set; }

    #region Unity Lifecycle & Initialization

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Don't initialize location manager here - let Unity call Start() naturally
            // This ensures proper initialization order
        }
        else
        {
            Destroy(gameObject); // Kill duplicates
            return;
        }
    }

    private void Start()
    {
        // Subscribe to map events
        EnsureMapManager();
        if (mapManager != null)
        {
            MapInteractionManager.OnGuessSubmitted += OnGuessSubmitted;
            MapInteractionManager.OnScoreCalculated += OnScoreCalculated;
            MapInteractionManager.OnMapOpened += OnMapOpened;
            MapInteractionManager.OnMapClosed += OnMapClosed;
        }

        // Try to find LocationManager if not assigned
        if (locationManager == null)
        {
            LogWarning("LocationManager not assigned in Inspector. Attempting to find it in scene...");
            locationManager = FindAnyObjectByType<LocationManager>();
            if (locationManager == null)
            {
                LogError("LocationManager not found in scene! Please ensure LocationManager component exists and is enabled.");
                return;
            }
            else
            {
                LogDebug("LocationManager found in scene.");
            }
        }

        // Ensure LocationManager is initialized
        if (locationManager != null)
        {
            // Force initialization if not already done
            if (!locationManager.IsInitialized())
            {
                LogWarning("LocationManager not yet initialized. Attempting to initialize...");
                // Try to call Start manually if it hasn't been called
                locationManager.Start();

                // Check again after manual initialization
                if (!locationManager.IsInitialized())
                {
                    LogWarning("LocationManager still not initialized. Will retry in next frame.");
                    // Use coroutine to wait for LocationManager to initialize
                    StartCoroutine(WaitForLocationManagerAndResolveMapPack());
                }
                else
                {
                    // Get map pack dictionary after LocationManager has initialized
                    allMapPacks = locationManager.GetMapPackDict();

                    // Validate MapPack name at startup
                    if (!ResolveMapPackId())
                    {
                        LogError($"Invalid MapPack name '{mapPackName}' in Inspector - please check the MapPack Name field");
                    }
                    else
                    {
                        LogDebug($"GameLogic: MapPack resolved successfully - '{mapPackName}' -> ID: {resolvedMapPackId}");
                    }
                }
            }
            else
            {
                // Get map pack dictionary after LocationManager has initialized
                allMapPacks = locationManager.GetMapPackDict();

                // Validate MapPack name at startup
                if (!ResolveMapPackId())
                {
                    LogError($"Invalid MapPack name '{mapPackName}' in Inspector - please check the MapPack Name field");
                }
                else
                {
                    LogDebug($"GameLogic: MapPack resolved successfully - '{mapPackName}' -> ID: {resolvedMapPackId}");
                }
            }
        }
        else
        {
            LogError("LocationManager not assigned and could not be found in scene!");
        }

        // Check current scene and hide map if we're in MenuScene_MonashClayton
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name == "MenuScene_MonashClayton")
        {
            if (MapInteractionManager.Instance != null)
            {
                MapInteractionManager.Instance.HideMap();
            }
            LogDebug("Started in MenuScene_MonashClayton - Map hidden");
        }

        // Initialise game only in actual game scenes
        if (currentScene.name != "MenuScene_MonashClayton" 
            && currentScene.name != "MenuScene_MonashCollege"
            && currentScene.name != "Map_selection")
        {
            InitializeGame();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (mapManager != null)
        {
            MapInteractionManager.OnGuessSubmitted -= OnGuessSubmitted;
            MapInteractionManager.OnScoreCalculated -= OnScoreCalculated;
            MapInteractionManager.OnMapOpened -= OnMapOpened;
            MapInteractionManager.OnMapClosed -= OnMapClosed;
        }
    }

    private void Update()
    {
        // Allow spacebar to advance from end-round state
        if (isRoundActive && !isGuessing && Input.GetKeyDown(KeyCode.Space))
        {
            HandleNextRoundButtonPressed();
        }
    }

    void OnEnable()
    {
        Debug.Log("OnEnable called");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    #endregion

    private void EnsureMapManager()
    {
        if (mapManager == null)
        {
            mapManager = FindAnyObjectByType<MapInteractionManager>();
            if (mapManager == null)
            {
                LogError("MapInteractionManager not found in scene!");
            }
            else
            {
                LogDebug("MapInteractionManager found in scene.");
            }
        }
    }

    #region Scene Management

    /// <summary>
    /// Loads the game scene from main menu
    /// </summary>
    public void LoadGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    /// <summary> Loads the starting map selection scene </summary>
    public void LoadMapSelection()
    {
        // Reset game state
        isGameActive = false;
        isRoundActive = false;
        inGame = false;
        isGuessing = false;
        currentScore = 0;
        currentRound = 1;

        // Hide map if it's currently shown
        if (MapInteractionManager.Instance != null)
        {
            MapInteractionManager.Instance.HideMap();
        }

        LogDebug("Returning to menu - game state reset");
        
        // Load menu scene
        SceneManager.LoadScene("Map_selection");
    }

    /// <summary>
    /// Called when a scene is loaded - manages map visibility and game initialization
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene loaded: " + scene.name);

        // Hide map when MenuScene_MonashClayton is loaded
        if (scene.name == "MenuScene_MonashClayton")
        {
            if (MapInteractionManager.Instance != null)
            {
                MapInteractionManager.Instance.HideMap();
            }
            LogDebug("MenuScene_MonashClayton loaded - Minimap UI hidden");
        }
        // Show map and initialize game when GameScene is loaded
        else if (scene.name == "GameScene")
        {
            LogDebug("GameScene loaded - Initializing game...");
            StartCoroutine(InitialiseGameScene());
        }
    }
    
    /// <summary>
    /// Initialises the game scene - resolves map pack, loads textures, then starts the first round
    /// </summary>
    private IEnumerator InitialiseGameScene()
    {
        EnsureMapManager();

        isGameActive = true;
        inGame = true;
        currentRound = 1;
        currentScore = 0;
        isGuessing = true;
        isRoundActive = false;
        scoreData.ResetAll();

        if (locationManager == null || !ResolveMapPackId())
        {
            LogError("Failed to resolve MapPack in InitialiseGameScene - cannot start game");
            yield break;
        }

        locationManager.SetCurrentMapPack(resolvedMapPackId);
        yield return StartCoroutine(locationManager.LoadMaterialsForMapPack(locationManager.GetCurrentMapPack()));
        if (MapInteractionManager.Instance != null)
            MapInteractionManager.Instance.SetMapCenter(pendingMapCenterLat, pendingMapCenterLng, pendingMapZoom, pendingCampusId);
        nextRound();
        LogDebug("GameScene loaded - textures loaded, game initialized");
    }
    #endregion

    #region Game Initialization

    /// <summary>
    /// Initializes the game
    /// </summary>
    private void InitializeGame()
    {
        currentRound = 1;
        currentScore = 0;
        isGameActive = true;
        inGame = true;

        LogDebug("Game initialized");

        // Start first round
        nextRound();
    }

    /// <summary>
    /// Restarts the game
    /// </summary>
    public void RestartGame()
    {
        currentRound = 1;
        currentScore = 0;
        isGameActive = true;
        isRoundActive = false;
        inGame = true;

        // Reset map manager - use singleton to ensure consistency
        if (MapInteractionManager.Instance != null)
        {
            MapInteractionManager.Instance.ResetRound();
        }

        // Hide results UI
        if (resultsUI != null)
        {
            resultsUI.SetActive(false);
        }

        LogDebug("Game restarted");
        nextRound();
    }

    #endregion

    #region Round Management

    /// <summary>
    /// Starts a new round
    /// </summary>
    public void nextRound()
    {
        if (!isGameActive)
        {
            LogDebug("nextRound() exiting: game not active");
            return;
        }
        if (isRoundActive)
        {
            LogDebug("nextRound() exiting: round already active");
            return;
        }

        isRoundActive = true;
        isGuessing = true;
        LogDebug($"nextRound() called | isGameActive:{isGameActive} isRoundActive:{isRoundActive} currentRound:{currentRound}");

        if (MapInteractionManager.Instance != null)
        {
            MapInteractionManager.Instance.ClearWebMapState();
        }

        // Get random location from location manager
        if (locationManager != null)
        {
            // Resolve MapPack name to ID if not already resolved
            if (resolvedMapPackId == -1 && !ResolveMapPackId())
            {
                LogError("Failed to resolve MapPack - cannot start round");
                return;
            }

            locationManager.SetCurrentMapPack(resolvedMapPackId);
            var currentMapPack = locationManager.GetCurrentMapPack();
            LogDebug($"MapPack set to: {currentMapPack.Name} (ID: {resolvedMapPackId})");

            locationManager.SelectRandomLocation();

            LocationManager.Location location;

            // Make it so the same location can't be selected twice in the same session


            do
            {
                locationManager.SelectRandomLocation();
                location = locationManager.GetCurrentLocation();
                LogDebug($"Attempt Location Change - Location: {location.Name}");
            } while (scoreData.PreviousLocations.Contains(location));

            scoreData.AddLocation(location);

            Debug.Log(scoreData.PreviousLocations);

            if (!string.IsNullOrEmpty(location.Name))
            {
                // Ensure map manager reference is valid before setting actual location
                if (mapManager == null)
                {
                    EnsureMapManager();
                }

                if (mapManager != null)
                {
                    LogDebug("nextRound(): calling SetActualLocation");
                    // Use singleton instance to ensure SubmitGuess (called from JS) operates on the same instance
                    MapInteractionManager.Instance.SetActualLocation(location.latitude, location.longitude, location.zLevel);
                }
                else
                {
                    LogError("MapInteractionManager not found when setting actual location; score will be unavailable.");
                }

                LogDebug($"Round {currentRound} started - Location: {location.Name} | latitude:{location.latitude}, longitude:{location.longitude}, zLevel={location.zLevel}");
            }
        }

        // Show map after actual location is set
        if (MapInteractionManager.Instance != null)
        {
            MapInteractionManager.Instance.ShowMap();
            MapInteractionManager.Instance.SetWebGuessingState(true);
            MapInteractionManager.Instance.SetWebMapSize("mm-size-s");
        }

        OnRoundStarted?.Invoke(currentRound);
        OnRoundUpdated?.Invoke(currentRound, totalRounds);
        LogDebug($"Round {currentRound} started");
    }

    /// <summary>
    /// Ends the current round
    /// </summary>
    public void EndRound()
    {
        // Only end the round if isRoundActive is false (triggered by Next Round button)
        if (isRoundActive)
        {
            // Do not end round yet, wait for Next Round button
            return;
        }

        isGuessing = false;

        // Hide map - use singleton to ensure consistency
        if (MapInteractionManager.Instance != null)
        {
            MapInteractionManager.Instance.HideMap();
        }

        OnRoundEnded?.Invoke(currentScore);
        LogDebug($"Round {currentRound} ended - Score: {currentScore}");

        if (MapInteractionManager.Instance != null)
        {
            MapInteractionManager.Instance.ResetRound();
        }

        // Start next round after delay
        StartNextRoundRoutine();

    }

    /// <summary>
    /// Call this from your Next Round button handler to end the round and start the next one
    /// </summary>
    public void OnNextRoundButtonPressed()
    {
        if (Instance != null && Instance != this)
        {
            Instance.HandleNextRoundButtonPressed();
            return;
        }

        HandleNextRoundButtonPressed();
    }

    /// <summary>
    /// Starts the next round after a delay
    /// </summary>
    private IEnumerator NextRoundDelay()
    {
        if (!skipNextRoundWait)
        {
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space)); // Wait for Space to go to next Round
        }
        skipNextRoundWait = false;

        // yield return new WaitForSeconds(2f); // 1 second delay 

        // Check if game is complete
        if (currentRound >= totalRounds)
        {
            EndGame();
            yield break;
        }

        currentRound++;
        OnRoundUpdated?.Invoke(currentRound, totalRounds);
        nextRound();
    }

    private void HandleNextRoundButtonPressed()
    {
        skipNextRoundWait = true;
        isRoundActive = false;
        EndRound();
    }

    private void StartNextRoundRoutine()
    {
        if (isActiveAndEnabled)
        {
            StartCoroutine(NextRoundDelay());
            return;
        }

        if (Instance != null && Instance.isActiveAndEnabled)
        {
            Instance.StartCoroutine(NextRoundDelay());
            return;
        }

        LogError("Cannot start NextRoundDelay because GameLogic is inactive.");
    }

    /// <summary>
    /// Ends the entire game
    /// </summary>
    private void EndGame()
    {
        isGameActive = false;
        isRoundActive = false;
        inGame = false;

        // Hide map
        if (MapInteractionManager.Instance != null)
        {
            MapInteractionManager.Instance.HideMap();
        }

        OnGameEnded?.Invoke(currentScore);
        LogDebug($"Game ended - Final Score: {currentScore}");

        // Show results or return to menu
        SceneManager.LoadScene("BreakdownScene");
        ShowResults();
    }

    #endregion

    #region Game Actions

    /// <summary>
    /// Submits guess through MapInteractionManager
    /// </summary>
    public void submitGuess()
    {
        if (mapManager != null)
        {
            // Let MapInteractionManager handle the guess submission
            // This will trigger OnGuessSubmitted and OnScoreCalculated events
            LogDebug("Guess submission delegated to MapInteractionManager");
        }
        else
        {
            LogWarning("MapInteractionManager not assigned - cannot submit guess");
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called when a guess is submitted
    /// </summary>
    /// <param name="guessLocation">The guessed location data with z-level support</param>
    private void OnGuessSubmitted(MapInteractionManager.LocationData guessLocation)
    {
        isGuessing = false;
        LogDebug($"Guess submitted at: latitude:{guessLocation.latitude}, longitude:{guessLocation.longitude}, zLevel: {guessLocation.zLevelName}");
        if (MapInteractionManager.Instance != null && locationManager != null)
        {
            var location = locationManager.GetCurrentLocation();
            MapInteractionManager.Instance.SendActualLocationToJavaScript(location.latitude, location.longitude, location.zLevel);
            MapInteractionManager.Instance.ShowBothLocations();
            MapInteractionManager.Instance.ShowMap();
            MapInteractionManager.Instance.SetWebGuessingState(false);
            MapInteractionManager.Instance.SetWebMapSize("mm-size-round-end");
        }
        // The score calculation will be handled by OnScoreCalculated
    }

    /// <summary>
    /// Called when score is calculated
    /// </summary>
    /// <param name="score">The calculated score</param>
    private void OnScoreCalculated(int score, int distance)
    {
        currentScore += score;
        
        LogDebug($"Score calculated: {score}, Total: {currentScore}");

        LogDebug($"ScoreData reference: {scoreData}");
        //Set current score to shared assets
        if (scoreData != null)
        {
            scoreData.SetTotalScore(currentScore);
            scoreData.SetRoundScore(score);
            scoreData.SetDistanceScore(distance);
            scoreData.AddScore(score);
        }

        OnScoreUpdated?.Invoke(currentScore);
        // End the round
        EndRound();
    }

    /// <summary>
    /// Called when map is opened
    /// </summary>
    private void OnMapOpened()
    {
        LogDebug("Minimap UI opened");
        // Update UI to show map is active
    }

    /// <summary>
    /// Called when map is closed
    /// </summary>
    private void OnMapClosed()
    {
        LogDebug("Minimap UI closed");
        // Update UI to show map is not active
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Shows game results
    /// </summary>
    private void ShowResults()
    {
        // Update score display
        if (MapInteractionManager.Instance != null)
        {
            MapInteractionManager.Instance.UpdateScoreDisplay(currentScore, currentRound);
        }

        // Show results UI if available
        if (resultsUI != null)
        {
            resultsUI.SetActive(true);
        }

        LogDebug("Results displayed");
    }

    #endregion

    #region Public Getters

    // Public getters for UI
    public int GetCurrentRound() => currentRound;
    public int GetTotalRounds() => totalRounds;
    public int GetCurrentScore() => currentScore;
    public int GetMapPackId() => resolvedMapPackId;
    public string GetMapPackName() => mapPackName;
    public string[] GetAllMapPackNames() => locationManager?.GetAllMapPackNames() ?? new string[0];
    public string GetMapPackNameById(int id) => locationManager?.GetMapPackNameById(id) ?? "Unknown";
    public bool IsGameActive() => isGameActive;
    public bool IsRoundActive() => isRoundActive;
    public bool IsGuessing() => isGuessing;
    public bool IsInGame() => inGame;
    public LocationManager GetLocationManager() => locationManager;

    /// <summary>
    /// Sets the MapPack by name and resolves it to an ID
    /// </summary>
    /// <param name="name">MapPack name</param>
    /// <returns>True if successful, false if MapPack not found</returns>
    public bool SetMapPackByName(string name)
    {
        if (locationManager == null)
        {
            LogError("LocationManager not assigned - cannot set MapPack");
            return false;
        }

        int id = locationManager.GetMapPackIdByName(name);
        if (id == -1)
        {
            LogError($"MapPack '{name}' not found. Available MapPacks: {string.Join(", ", locationManager.GetAllMapPackNames())}");
            return false;
        }

        mapPackName = name;
        resolvedMapPackId = id;

        var dict = locationManager.GetMapPackDict();
        if (dict != null && dict.ContainsKey(id))
            pendingCampusId = dict[id].campusId != 0 ? dict[id].campusId : 159;

        LogDebug($"MapPack set to: {name} (ID: {id}, campusId: {pendingCampusId})");

        // Fire MapPack changed event
        OnMapPackChanged?.Invoke(name);

        return true;
    }

    /// <summary>
    /// Gets the current MapPack name from the Inspector field
    /// </summary>
    /// <returns>Current MapPack name</returns>
    public string GetCurrentMapPackName() => mapPackName;

    /// <summary>
    /// Validates and resolves the current MapPack name to ID
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    private bool ResolveMapPackId()
    {
        if (locationManager == null)
        {
            LogError("LocationManager not assigned - cannot resolve MapPack");
            return false;
        }

        // Log the actual value being used
        LogDebug($"GameLogic: Resolving MapPack name '{mapPackName}' to ID... (mapPackName field value: '{mapPackName}')");
        int id = locationManager.GetMapPackIdByName(mapPackName);
        if (id == -1)
        {
            string[] availableMapPacks = locationManager.GetAllMapPackNames();
            LogError($"GameLogic: MapPack '{mapPackName}' not found. Available MapPacks: {string.Join(", ", availableMapPacks)}");
            return false;
        }

        resolvedMapPackId = id;

        var dict = locationManager.GetMapPackDict();
        if (dict != null && dict.ContainsKey(id))
            pendingCampusId = dict[id].campusId != 0 ? dict[id].campusId : 159;

        LogDebug($"GameLogic: MapPack '{mapPackName}' resolved to ID: {id}, campusId: {pendingCampusId}");

        // Fire MapPack changed event (only if this is not the initial startup resolution)
        if (mapPackName != "all" || resolvedMapPackId != 0) // Avoid firing for default startup
        {
            OnMapPackChanged?.Invoke(mapPackName);
        }

        return true;
    }

    /// <summary>
    /// Coroutine to wait for LocationManager to initialize before resolving map pack
    /// </summary>
    private IEnumerator WaitForLocationManagerAndResolveMapPack()
    {
        int maxWaitFrames = 60; // Wait up to 1 second at 60fps
        int frameCount = 0;

        while (!locationManager.IsInitialized() && frameCount < maxWaitFrames)
        {
            yield return null; // Wait one frame
            frameCount++;
        }

        if (locationManager.IsInitialized())
        {
            LogDebug("LocationManager initialized - resolving map pack...");
            // Get map pack dictionary after LocationManager has initialized
            allMapPacks = locationManager.GetMapPackDict();

            // Validate MapPack name at startup
            if (!ResolveMapPackId())
            {
                LogError($"Invalid MapPack name '{mapPackName}' in Inspector - please check the MapPack Name field");
            }
            else
            {
                LogDebug($"GameLogic: MapPack resolved successfully - '{mapPackName}' -> ID: {resolvedMapPackId}");
            }
        }
        else
        {
            LogError("LocationManager failed to initialize after waiting. Check LocationManager component.");
        }
    }
    /// <summary>
    /// Sets the pending map center to be applied when the game scene loads
    /// </summary>
    public void SetPendingMapCenter(float lat, float lng, int zoom)
    {
        pendingMapCenterLat = lat;
        pendingMapCenterLng = lng;
        pendingMapZoom = zoom;
        LogDebug($"Pending map center set to: {lat}, {lng}, zoom: {zoom}");
    }
    #endregion

    #region Debug Logging

    // Debug logging methods
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[GameLogic] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[GameLogic] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[GameLogic] {message}");
    }

    #endregion
}