using UnityEngine;

/// <summary>
/// Utility script for spawning and managing UI prefabs at runtime.
/// Supports multiple prefab types and automatic cleanup.
/// </summary>
public class SpawnPrefab : MonoBehaviour
{
    [Header("Prefab Configuration")]
    [SerializeField] private GameObject prefab;   // drag the PREFAB asset (blue) here
    [SerializeField] private Transform parent;    // drag the SCENE parent (e.g., your overlay or Canvas)
    [SerializeField] private bool preventMultiple = true;
    [SerializeField] private bool centerOnSpawn = true;
    [SerializeField] private bool setAsLastSibling = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private GameObject spawned;

    #region Public Methods

    /// <summary>
    /// Spawns the assigned prefab as a child of the parent transform
    /// </summary>
    public void Spawn()
    {
        if (!prefab) 
        { 
            LogError("SpawnPrefab: No prefab assigned."); 
            return; 
        }
        if (!parent) 
        { 
            LogError("SpawnPrefab: No parent assigned."); 
            return; 
        }
        if (preventMultiple && spawned) 
        {
            LogDebug("SpawnPrefab: Prefab already spawned and preventMultiple is enabled.");
            return;
        }

        // For UI prefabs, 'false' keeps RectTransform correct under parent
        spawned = Instantiate(prefab, parent, false);
        
        if (setAsLastSibling)
            spawned.transform.SetAsLastSibling(); // on top

        // Center the UI element if requested
        if (centerOnSpawn && spawned.TryGetComponent<RectTransform>(out var rt))
            rt.anchoredPosition = Vector2.zero;

        LogDebug($"SpawnPrefab: Spawned {prefab.name}");
    }

    /// <summary>
    /// Despawns the currently spawned prefab
    /// </summary>
    public void Despawn()
    {
        if (spawned) 
        {
            LogDebug($"SpawnPrefab: Despawning {spawned.name}");
            Destroy(spawned);
            spawned = null;
        }
    }

    /// <summary>
    /// Toggles between spawn and despawn
    /// </summary>
    public void Toggle()
    {
        if (spawned)
            Despawn();
        else
            Spawn();
    }

    /// <summary>
    /// Checks if a prefab is currently spawned
    /// </summary>
    public bool IsSpawned => spawned != null;

    /// <summary>
    /// Gets the currently spawned GameObject (null if none)
    /// </summary>
    public GameObject GetSpawnedObject => spawned;

    #endregion

    #region Private Methods

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[SpawnPrefab] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SpawnPrefab] {message}");
    }

    #endregion
}
