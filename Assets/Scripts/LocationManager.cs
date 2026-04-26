using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;


/// <summary>
/// LocationManager, Responsible for handling location data, map packs, and location selection.
/// Manages JSON data loading, material assignment, and current location tracking.
/// 
/// Written by O-Bolt
/// Modified by aleu0007
/// Last Modified: 24/09/2025
/// </summary>
public class LocationManager : MonoBehaviour
{
    #region Data Structures

    /// <summary>
    /// Container for location and map pack data loaded from JSON
    /// </summary>
    [System.Serializable]
    public class locationData
    {
        public List<Location> Locations;
        public List<MapPack> MapPacks;
    }

    /// <summary>
    /// Represents a single location with ID, name, coordinates, and material
    /// </summary>
    [System.Serializable]
    public struct Location
    {
        // Each Location has an ID, name and location material
        // (Name will be the long form data presented on the Screen after a guess)
        public int ID;
        public string Name;
        public string FileName;
        public float latitude;  // Latitude (replaces x)
        public float longitude;  // Longitude (replaces y)
        public int zLevel; // Z-level (replaces z)

        [System.NonSerialized]
        // Link to the 360 Image of the Location
        public Material LocationMaterial;
    }

    /// <summary>
    /// Represents a collection of locations grouped together
    /// </summary>
    [System.Serializable]
    public struct MapPack
    {
        public int ID;
        public string Name;
        public List<int> locationIDs;
        public string bucketSubdirectory; // Subdirectory in R2 bucket for this MapPack (e.g., "monash-101"). Leave empty for root level.
    }

    #endregion

    #region Inspector Variables

    [Header("Data Configuration")]
    [SerializeField] private string jsonResourcePath = "locationData";

    [Header("Remote Image Configuration")]
    [Tooltip("Base URL for 360 images. Use your custom domain (360images.monashemerging.tech) for production, " +
             "or the public dev URL (https://pub-17f59a97c119414788b775aeebf13e76.r2.dev) for testing. " +
             "Leave empty to use only local Resources. Include https:// prefix.")]
    [SerializeField] private string imageBaseUrl = "https://pub-17f59a97c119414788b775aeebf13e76.r2.dev";
    
    [Tooltip("File extension for images (e.g., .jpg, .png). Will be appended to FileName.")]
    [SerializeField] private string imageFileExtension = ".jpg";
    
    [Tooltip("If remote fails, try Materials/Locations/ in Resources. Set false when all images are in the bucket.")]
    [SerializeField] private bool useLocalFallback = true;
    // WHEN ALL REMOTE: Remove useLocalFallback and its fallback logic in LoadLocationMaterialFromUrl().

    #endregion

    #region Private Variables

    private Dictionary<int, Location> locationDict;
    private Dictionary<int, MapPack> mapPackDict;
    private MapPack currentMapPack;
    private Location currentLocation;
    private Dictionary<int, bool> locationLoadingStatus; // Track which locations are currently loading
    private int totalLocationsToLoad = 0;
    private int locationsLoadedCount = 0;

    #endregion

    #region Public Getters

    /// <summary>
    /// Checks if LocationManager has been initialized with data
    /// </summary>
    /// <returns>True if initialized, false otherwise</returns>
    public bool IsInitialized()
    {
        return mapPackDict != null && locationDict != null;
    }

    public Location GetCurrentLocation() => currentLocation;
    public MapPack GetCurrentMapPack() => currentMapPack;
    public string GetCurrentMapPackName() => currentMapPack.Name;
    public Dictionary<int, Location> GetLocationDict() => locationDict;
    public Dictionary<int, MapPack> GetMapPackDict() => mapPackDict;

    /// <summary>
    /// Gets all available MapPack names
    /// </summary>
    /// <returns>Array of MapPack names</returns>
    public string[] GetAllMapPackNames()
    {
        if (mapPackDict == null)
        {
            Debug.LogError("LocationManager: GetAllMapPackNames() called before initialization. MapPack dictionary is null.");
            return new string[0];
        }
        return mapPackDict.Values.Select(mp => mp.Name).ToArray();
    }

    /// <summary>
    /// Gets MapPack name by ID
    /// </summary>
    /// <param name="id">MapPack ID</param>
    /// <returns>MapPack name or "Unknown" if not found</returns>
    public string GetMapPackNameById(int id)
    {
        if (mapPackDict == null)
        {
            Debug.LogError("LocationManager: GetMapPackNameById() called before initialization. MapPack dictionary is null.");
            return "Unknown";
        }
        return mapPackDict.ContainsKey(id) ? mapPackDict[id].Name : "Unknown";
    }

    /// <summary>
    /// Gets MapPack ID by name
    /// </summary>
    /// <param name="name">MapPack name</param>
    /// <returns>MapPack ID or -1 if not found</returns>
    public int GetMapPackIdByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return -1;

        if (mapPackDict == null)
        {
            Debug.LogError("LocationManager: GetMapPackIdByName() called before initialization. MapPack dictionary is null.");
            return -1;
        }

        foreach (var kvp in mapPackDict)
        {
            if (kvp.Value.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Key;
            }
        }
        return -1;
    }

    /// <summary>
    /// Validates if a MapPack name exists
    /// </summary>
    /// <param name="name">MapPack name to validate</param>
    /// <returns>True if MapPack exists, false otherwise</returns>
    public bool IsValidMapPackName(string name)
    {
        return GetMapPackIdByName(name) != -1;
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the LocationManager by loading data from JSON
    /// </summary>
    public void Start()
    {
        Debug.Log("LocationManager: Initializing...");
        LoadData();
        Debug.Log($"LocationManager: Init complete. Loaded {locationDict?.Count ?? 0} locations, {mapPackDict?.Count ?? 0} map packs.");
    }

    #endregion

    #region Location Management

    /// <summary>
    /// Selects a random location from the current map pack and sets it as skybox
    /// </summary>
    public void SelectRandomLocation()
    {
        if (string.IsNullOrEmpty(currentMapPack.Name))
        {
            Debug.LogWarning("LocationManager: Cannot select random location - no map pack is set. Call SetCurrentMapPack() first.");
            return;
        }

        List<Location> locations = GetLocationsFromMapPack(currentMapPack);

        if (locations.Count == 0)
        {
            Debug.LogWarning($"LocationManager: No locations found in map pack '{currentMapPack.Name}'");
            return;
        }

        int randomIndex = Random.Range(0, locations.Count);
        var selectedLocation = locations[randomIndex];

        // Check if material is loaded before setting skybox
        if (selectedLocation.LocationMaterial == null)
        {
            Debug.LogError($"LocationManager: Material missing for '{selectedLocation.Name}' (ID:{selectedLocation.ID}), FileName:'{selectedLocation.FileName}'");
        }
        else
        {
            RenderSettings.skybox = selectedLocation.LocationMaterial;
            DynamicGI.UpdateEnvironment();
        }

        Debug.Log($"LocationManager: Location selected - ID:{selectedLocation.ID}, Name:{selectedLocation.Name}, latitude:{selectedLocation.latitude}, longitude:{selectedLocation.longitude}, z:{selectedLocation.zLevel}");
        SetCurrentLocation(selectedLocation);
    }

    /// <summary>
    /// Gets all locations from a specific map pack
    /// </summary>
    /// <param name="mapPack">The map pack to get locations from</param>
    /// <returns>List of locations in the map pack</returns>
    public List<Location> GetLocationsFromMapPack(MapPack mapPack)
    {
        List<Location> locations = new List<Location>();


        if (mapPack.Name == "all" || mapPack.Name == "")
        {
            locations = locationDict.Values.ToList();
            return locations;
        }

        foreach (int id in mapPack.locationIDs)
        {
            locations.Add(locationDict[id]);
        }

        return locations;
    }

    #endregion

    #region Setters

    /// <summary>
    /// Sets the current location
    /// </summary>
    /// <param name="location">The location to set as current</param>
    public void SetCurrentLocation(Location location)
    {
        currentLocation = location;
    }

    /// <summary>
    /// Sets the current map pack by ID
    /// </summary>
    /// <param name="id">The ID of the map pack to set</param>
    public void SetCurrentMapPack(int id)
    {
        if (mapPackDict == null || mapPackDict.Count == 0)
        {
            Debug.LogError("LocationManager: Cannot set map pack - map pack dictionary not initialized.");
            return;
        }

        if (mapPackDict.ContainsKey(id))
        {
            currentMapPack = mapPackDict[id];
            int locationCount = GetLocationsFromMapPack(currentMapPack).Count;
            Debug.Log($"LocationManager: MapPack set '{currentMapPack.Name}' (ID:{id}) locations:{locationCount}");
        }
        else
        {
            Debug.LogWarning($"LocationManager: MapPack ID {id} not found. Available IDs: {string.Join(", ", mapPackDict.Keys)}");
        }
    }

    #endregion

    #region Data Loading

    /*
     * LoadData flow:
     * 1. Load JSON from Resources, parse into locations + map packs
     * 2. Populate locationDict and mapPackDict
     * 3. Assign 360 materials: remote-first (fetch from URL, fall back to local if useLocalFallback).
     *    If imageBaseUrl is empty (e.g. prefab not serializing it), default R2 URL is used.
     *
     * WHEN ALL REMOTE: Set useLocalFallback = false and remove fallback logic in LoadLocationMaterialFromRemote.
     */

    /// <summary>
    /// Loads location and map pack data from JSON, then assigns 360 materials (remote-first, local fallback).
    /// </summary>
    private void LoadData()
    {
        // ---- 1. Load and parse JSON ----
        if (string.IsNullOrEmpty(jsonResourcePath))
        {
            Debug.LogError("LocationManager: JSON Data Resource path is not assigned.");
            return;
        }

        TextAsset jsonFile = Resources.Load<TextAsset>(jsonResourcePath);
        if (jsonFile == null)
        {
            Debug.LogError($"LocationManager: JSON not found at Resources/{jsonResourcePath}");
            return;
        }

        locationData data = JsonUtility.FromJson<locationData>(jsonFile.text);
        if (data == null || data.Locations == null || data.MapPacks == null)
        {
            Debug.LogError("LocationManager: Failed to parse JSON or missing Locations/MapPacks.");
            return;
        }

        // ---- 2. Populate dictionaries ----
        locationDict = new Dictionary<int, Location>();
        mapPackDict = new Dictionary<int, MapPack>();

        foreach (Location loc in data.Locations)
            locationDict.Add(loc.ID, loc);
        foreach (MapPack mp in data.MapPacks)
            mapPackDict.Add(mp.ID, mp);

        Debug.Log($"LocationManager: Loaded {locationDict.Count} locations, {mapPackDict.Count} map packs.");

        // ---- 3. Assign materials: remote-first or local-only ----
        // Prefab may not serialize imageBaseUrl; if empty, use default R2 URL so remote loading still works
        if (string.IsNullOrEmpty(imageBaseUrl))
        {
            imageBaseUrl = "https://pub-17f59a97c119414788b775aeebf13e76.r2.dev";
            Debug.Log("LocationManager: imageBaseUrl was empty - using default R2 URL.");
        }

        Debug.Log($"LocationManager: Loading from remote - base URL: {imageBaseUrl}");
        CoroutineRunner.Instance.StartCoroutine(AssignLocationMaterialsFromRemote());
    }

    /// <summary>
    /// Loads materials from Resources/Materials/Locations/. Used when imageBaseUrl is empty (local-only mode).
    /// WHEN ALL REMOTE: Can be removed once imageBaseUrl is always set.
    /// </summary>
    private void AssignLocationMaterialsLocalOnly()
    {
        int failureCount = 0;

        foreach (Location location in locationDict.Values.ToList())
        {
            Location updatedLocation = location;
            string materialPath = $"Materials/Locations/{location.FileName}";

            updatedLocation.LocationMaterial = Resources.Load<Material>(materialPath);

            if (updatedLocation.LocationMaterial == null)
            {
                Debug.LogError($"LocationManager: Material not found for location '{location.Name}' (ID: {location.ID}), FileName: '{location.FileName}', Path: '{materialPath}'");
                failureCount++;
            }

            locationDict[location.ID] = updatedLocation;
        }

        if (failureCount > 0)
        {
            Debug.LogError($"LocationManager: Material assignment complete with {failureCount} failures");
        }
        else
        {
            Debug.Log("LocationManager: Material assignment complete with no failures");
        }
    }
    
    /// <summary>
    /// Fetches images from remote URL, creates skybox materials. Per-location fallback to local Resources if enabled.
    /// Flow: For each location: try remote, then if fail and useLocalFallback, try Resources.
    /// WHEN ALL REMOTE: Remove the fallback block; simplify to remote-only.
    /// </summary>
    private IEnumerator AssignLocationMaterialsFromRemote()
    {
        locationLoadingStatus = new Dictionary<int, bool>();
        totalLocationsToLoad = locationDict.Count;
        locationsLoadedCount = 0;

        Debug.Log($"LocationManager: Loading {totalLocationsToLoad} textures from remote (fallback: {(useLocalFallback ? "local" : "none")})");

        // Load one location at a time in this coroutine (avoids starting 46 coroutines on an object that may become inactive)
        foreach (Location location in locationDict.Values.ToList())
        {
            locationLoadingStatus[location.ID] = false;
            yield return LoadLocationMaterialFromRemote(location);
        }

        int successCount = locationLoadingStatus.Values.Count(status => status);
        int failureCount = totalLocationsToLoad - successCount;

        if (failureCount > 0)
        {
            Debug.LogWarning($"LocationManager: Material loading complete. {successCount} succeeded, {failureCount} failed.");
        }
        else
        {
            Debug.Log($"LocationManager: Material loading complete. All {successCount} locations loaded successfully.");
        }
    }

    /// <summary>
    /// Gets the bucket subdirectory for a location by finding which MapPack it belongs to
    /// Only uses specific MapPacks (skips "all" since it's just a logical grouping, not a bucket subdirectory)
    /// </summary>
    private string GetBucketSubdirectoryForLocation(int locationID)
    {
        if (mapPackDict == null) return "";

        // Find the first specific MapPack (not "all") that contains this location
        foreach (var mapPack in mapPackDict.Values)
        {
            // Skip "all" MapPack - it's just a logical grouping, not a bucket subdirectory
            if (mapPack.Name.ToLower() == "all")
            {
                continue;
            }

            if (mapPack.locationIDs != null && mapPack.locationIDs.Contains(locationID))
            {
                return mapPack.bucketSubdirectory ?? "";
            }
        }

        // If location is only in "all" MapPack, return empty (root level)
        // This shouldn't happen if all locations are properly assigned to specific MapPacks
        return "";
    }

    /// <summary>
    /// 1. Fetch image from remote URL. 2. Create skybox material. 3. If remote fails and useLocalFallback, load from Resources.
    /// WHEN ALL REMOTE: Remove the fallback block (lines that check useLocalFallback and Resources.Load).
    /// </summary>
    private IEnumerator LoadLocationMaterialFromRemote(Location location)
    {
        // Get bucket subdirectory from the MapPack that contains this location
        string subdirectory = GetBucketSubdirectoryForLocation(location.ID);
        
        // Construct URL with bucket subdirectory
        string imageUrl;
        if (string.IsNullOrEmpty(subdirectory))
        {
            // No subdirectory (root level)
            imageUrl = $"{imageBaseUrl.TrimEnd('/')}/{location.FileName}{imageFileExtension}";
        }
        else
        {
            // Include bucket subdirectory
            imageUrl = $"{imageBaseUrl.TrimEnd('/')}/{subdirectory.Trim('/')}/{location.FileName}{imageFileExtension}";
        }
        
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return www.SendWebRequest();

            Location updatedLocation = location;
            bool materialLoaded = false;

            // ---- 1. Remote: fetch from URL, create skybox material ----
            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                if (texture != null)
                {
                    Material skyboxMaterial = CreateSkyboxMaterial(location, texture);
                    
                    if (skyboxMaterial != null)
                    {
                        updatedLocation.LocationMaterial = skyboxMaterial;
                        locationDict[location.ID] = updatedLocation;
                        locationLoadingStatus[location.ID] = true;
                        materialLoaded = true;
                        Debug.Log($"LocationManager: Successfully loaded from bucket: '{location.Name}' (ID: {location.ID})");
                    }
                }
            }

            // ---- 2. Fallback: local Resources (WHEN ALL REMOTE: remove this block) ----
            if (!materialLoaded && useLocalFallback)
            {
                string materialPath = $"Materials/Locations/{location.FileName}";
                updatedLocation.LocationMaterial = Resources.Load<Material>(materialPath);
                
                if (updatedLocation.LocationMaterial != null)
                {
                    locationDict[location.ID] = updatedLocation;
                    locationLoadingStatus[location.ID] = true;
                    materialLoaded = true;
                    Debug.Log($"LocationManager: Using local fallback for '{location.Name}' (ID: {location.ID}) - not found in bucket");
                }
            }

            // ---- 3. Report failure ----
            if (!materialLoaded)
            {
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"LocationManager: Failed to load from bucket for '{location.Name}' (ID: {location.ID}): {www.error}");
                }
                
                if (!useLocalFallback)
                {
                    Debug.LogError($"LocationManager: No local fallback enabled. Material missing for '{location.Name}' (ID: {location.ID})");
                }
                else
                {
                    Debug.LogError($"LocationManager: Both remote and local fallback failed for '{location.Name}' (ID: {location.ID})");
                }
                
                locationLoadingStatus[location.ID] = false;
            }
        }

        locationsLoadedCount++;
    }

    /// <summary>
    /// Creates a skybox material from a downloaded texture. Tries cloning from Resources for shader settings, else creates new.
    /// WHEN ALL REMOTE: The Resources clone is optional; you can remove it and always create from Skybox/Panoramic shader.
    /// </summary>
    private Material CreateSkyboxMaterial(Location location, Texture2D texture)
    {
        Material skyboxMaterial = null;

        // Optional: clone local material for shader settings (WHEN ALL REMOTE: can remove, use shader creation only)
        Material refMaterial = Resources.Load<Material>($"Materials/Locations/{location.FileName}");
        if (refMaterial != null)
        {
            skyboxMaterial = new Material(refMaterial);
            Debug.Log($"LocationManager: Cloned material from Resources for '{location.Name}' (ID: {location.ID})");
        }

        // If no template found, create a new material with common skybox shaders
        if (skyboxMaterial == null)
        {
            // Try common 360/skybox shaders
            Shader skyboxShader = Shader.Find("Skybox/Panoramic") ?? 
                                Shader.Find("Skybox/6 Sided") ?? 
                                Shader.Find("Skybox/Cubemap");
            
            if (skyboxShader != null)
            {
                skyboxMaterial = new Material(skyboxShader);
                Debug.Log($"LocationManager: Created new material with shader '{skyboxShader.name}' for '{location.Name}' (ID: {location.ID})");
            }
        }

        if (skyboxMaterial != null)
        {
            // Set texture - try common property names
            if (skyboxMaterial.HasProperty("_MainTex"))
            {
                skyboxMaterial.SetTexture("_MainTex", texture);
            }
            else if (skyboxMaterial.HasProperty("_Tex"))
            {
                skyboxMaterial.SetTexture("_Tex", texture);
            }
            else if (skyboxMaterial.HasProperty("_FrontTex"))
            {
                // For 6-sided skybox, set all faces to the same texture
                skyboxMaterial.SetTexture("_FrontTex", texture);
                skyboxMaterial.SetTexture("_BackTex", texture);
                skyboxMaterial.SetTexture("_LeftTex", texture);
                skyboxMaterial.SetTexture("_RightTex", texture);
                skyboxMaterial.SetTexture("_UpTex", texture);
                skyboxMaterial.SetTexture("_DownTex", texture);
            }
            else
            {
                // Try to set the first texture property found
                var shader = skyboxMaterial.shader;
                for (int i = 0; i < shader.GetPropertyCount(); i++)
                {
                    if (shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                    {
                        string propName = shader.GetPropertyName(i);
                        skyboxMaterial.SetTexture(propName, texture);
                        Debug.Log($"LocationManager: Set texture property '{propName}' for '{location.Name}' (ID: {location.ID})");
                        break;
                    }
                }
            }
        }

        return skyboxMaterial;
    }

    /*
     * WHEN ALL ASSETS ARE REMOTE - cleanup checklist:
     * 1. Inspector: Remove useLocalFallback (or set false and ignore).
     * 2. LoadData: Remove local-only branch; always call AssignLocationMaterialsFromRemote().
     * 3. AssignLocationMaterialsLocalOnly: Delete entire method.
     * 4. LoadLocationMaterialFromRemote: Delete the fallback block (Resources.Load when remote fails).
     * 5. CreateSkyboxMaterial: Optionally remove Resources clone; always create from Skybox/Panoramic.
     * 6. Simplify error messages to remote-only (no "both failed" cases).
     */

    #endregion

    #region Legacy Methods (Deprecated)

    /// <summary>
    /// Legacy method - use GetLocationsFromMapPack instead
    /// </summary>
    [System.Obsolete("Use GetLocationsFromMapPack instead")]
    public List<Location> getLocationsFromMapPack(MapPack mapPack)
    {
        return GetLocationsFromMapPack(mapPack);
    }

    /// <summary>
    /// Legacy method - use SetCurrentLocation instead
    /// </summary>
    [System.Obsolete("Use SetCurrentLocation instead")]
    public void setCurrentLocation(Location location)
    {
        SetCurrentLocation(location);
    }

    /// <summary>
    /// Legacy method - use SetCurrentMapPack instead
    /// </summary>
    [System.Obsolete("Use SetCurrentMapPack instead")]
    public void setCurrentMapPack(int id)
    {
        SetCurrentMapPack(id);
    }

    #endregion
}