using UnityEngine;

/// <summary>
/// Manages the round result bottom bar that appears at the end of each round.
/// Handles spawning/despawning of the round result bar in the GameScene.
/// The play again bar is handled separately in the BreakdownScene.
/// 
/// Written by Assistant
/// Last Modified: [Current Date]
/// </summary>
public class RoundResultBarManager : MonoBehaviour
{
    [Header("Round Result Bar")]
    [SerializeField] private GameObject roundResultPrefab;
    
    [Header("UI Configuration")]
    [SerializeField] private Transform canvasParent;
    [SerializeField] private bool enableDebugLogs = true;
    
    // SpawnPrefab component for the round result bar
    private SpawnPrefab roundResultSpawner;
    
    #region Unity Lifecycle
    
    private void Awake()
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
                LogError("No Canvas found! Please assign a Canvas as the parent for bottom bars.");
            }
        }
        
        SetupSpawners();
    }
    
    private void Start()
    {
        // Subscribe to GameLogic events
        if (GameLogic.Instance != null)
        {
            GameLogic.OnRoundStarted += OnRoundStarted;
            GameLogic.OnScoreUpdated += OnScoreUpdated;
        }
        else
        {
            LogError("GameLogic instance not found! Round result bar will not work properly.");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        GameLogic.OnRoundStarted -= OnRoundStarted;
        GameLogic.OnScoreUpdated -= OnScoreUpdated;
    }
    
    #endregion
    
    #region Event Handlers
    
    /// <summary>
    /// Called when a new round starts - hide the round result bar
    /// </summary>
    private void OnRoundStarted(int roundNumber)
    {
        LogDebug($"Round {roundNumber} started - hiding round result bar");
        HideRoundResultBar();
    }
    

    private void OnScoreUpdated(int score)
    {
        LogDebug("Guess submitted - showing round result bar");
        ShowRoundResultBar();
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Shows the round result bottom bar
    /// </summary>
    public void ShowRoundResultBar()
    {
        if (roundResultSpawner != null)
        {
            roundResultSpawner.Spawn();
        }
        else
        {
            LogError("Round result spawner not set up!");
        }
    }
    
    /// <summary>
    /// Hides the round result bar
    /// </summary>
    public void HideRoundResultBar()
    {
        if (roundResultSpawner != null)
            roundResultSpawner.Despawn();
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Sets up the SpawnPrefab component for the round result bar
    /// </summary>
    private void SetupSpawners()
    {
        // Create round result spawner
        if (roundResultPrefab != null)
        {
            GameObject roundResultSpawnerObj = new GameObject("RoundResultSpawner");
            roundResultSpawnerObj.transform.SetParent(transform);
            roundResultSpawner = roundResultSpawnerObj.AddComponent<SpawnPrefab>();
            
            // Configure the spawner
            var spawnerScript = roundResultSpawnerObj.GetComponent<SpawnPrefab>();
            SetSpawnerPrefab(spawnerScript, roundResultPrefab, canvasParent);
        }
        else
        {
            LogError("Round result prefab not assigned!");
        }
    }
    
    /// <summary>
    /// Sets the prefab and parent for a SpawnPrefab component using reflection
    /// </summary>
    private void SetSpawnerPrefab(SpawnPrefab spawner, GameObject prefab, Transform parent)
    {
        // Use reflection to set private fields
        var prefabField = typeof(SpawnPrefab).GetField("prefab", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var parentField = typeof(SpawnPrefab).GetField("parent", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (prefabField != null) prefabField.SetValue(spawner, prefab);
        if (parentField != null) parentField.SetValue(spawner, parent);
    }
    
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[BottomBarManager] {message}");
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[BottomBarManager] {message}");
    }
    
    #endregion
}
