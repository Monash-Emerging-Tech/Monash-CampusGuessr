using UnityEngine;

/// <summary>
/// Sends Unity commands to the WebGL MazeMap JavaScript layer.
/// </summary>
public static class WebMapBridge
{
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

    public static void ShowMap(bool enableDebugLogs = true)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        showMapFromUnity();
#else
        LogDebug("Map would be shown (WebGL only)", enableDebugLogs);
#endif
    }

    public static void HideMap(bool enableDebugLogs = true)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        hideMapFromUnity();
#else
        LogDebug("Map would be hidden (WebGL only)", enableDebugLogs);
#endif
    }

    public static void AddActualLocation(string jsonPayload, float latitude, float longitude, int zLevel, bool enableDebugLogs = true)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        addActualLocationFromUnity(jsonPayload);
#else
        LogDebug($"Actual location would be sent to JavaScript: ({latitude}, {longitude}), Level: {ZLevelFormatter.GetName(zLevel)}", enableDebugLogs);
#endif
    }

    public static void SetGuessingState(bool isGuessing, bool enableDebugLogs = true)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        setGuessingStateFromUnity(isGuessing);
#else
        LogDebug($"Guessing state would be sent to JavaScript: {(isGuessing ? "Guessing" : "Results")}", enableDebugLogs);
#endif
    }

    public static void SetMapSize(string size, bool enableDebugLogs = true)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        mmSetWidgetSize(size);
#else
        LogDebug($"Map size would be sent to JavaScript: {size}", enableDebugLogs);
#endif
    }

    public static void ClearMapState(bool enableDebugLogs = true)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        clearMapStateFromUnity();
#else
        LogDebug("Map state would be cleared on JavaScript", enableDebugLogs);
#endif
    }

    public static void SetMapCenter(float lat, float lng, int zoom = 16, int campusId = 159, bool enableDebugLogs = true)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        setMapPackViewFromUnity(campusId, lat, lng, zoom);
#else
        LogDebug($"Map center would be set to: {lat}, {lng}, zoom: {zoom}, campusId: {campusId}", enableDebugLogs);
#endif
    }

    public static void UpdateScoreDisplay(int score, int round, bool enableDebugLogs = true)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        updateScoreFromUnity(score, round);
#else
        LogDebug($"Score would be updated: {score}, Round: {round}", enableDebugLogs);
#endif
    }

    public static void ShowLoading(bool show, bool enableDebugLogs = true)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        showLoading(show);
#else
        LogDebug($"Loading would be {(show ? "shown" : "hidden")}", enableDebugLogs);
#endif
    }

    public static void AddMarker(float lat, float lng, string label, string type, bool enableDebugLogs = true)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        addMarkerFromUnity(lat, lng, label, type);
#else
        LogDebug($"Marker would be added: {label} at {lat}, {lng} ({type})", enableDebugLogs);
#endif
    }

    private static void LogDebug(string message, bool enableDebugLogs)
    {
        if (!enableDebugLogs) return;

        Debug.Log($"[WebMapBridge] {message}");
    }
}
