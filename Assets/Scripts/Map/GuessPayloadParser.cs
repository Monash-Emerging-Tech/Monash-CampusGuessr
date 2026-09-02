#nullable enable
using System;
using UnityEngine;

/// <summary>
/// Converts map JSON payloads from JavaScript into Unity location and marker data.
/// </summary>
public static class GuessPayloadParser
{
    public readonly struct ParsedGuess
    {
        public ParsedGuess(
            MapInteractionManager.LocationData location,
            MapInteractionManager.MarkerData? marker,
            bool hasMarker,
            string logMessage
        )
        {
            Location = location;
            Marker = marker;
            HasMarker = hasMarker;
            LogMessage = logMessage;
        }

        public MapInteractionManager.LocationData Location { get; }
        public MapInteractionManager.MarkerData? Marker { get; }
        public bool HasMarker { get; }
        public string LogMessage { get; }
    }

    public static bool TryParseMapClick(string jsonData, int currentZLevel, out ParsedGuess parsedGuess)
    {
        // Try to parse as enhanced data first
        var enhancedData = JsonUtility.FromJson<EnhancedMapClickData>(jsonData);
        if (enhancedData != null && !string.IsNullOrEmpty(enhancedData.zLevelName))
        {
            // Enhanced data with z-level
            var location = new MapInteractionManager.LocationData
            {
                latitude = enhancedData.latitude,
                longitude = enhancedData.longitude,
                zLevel = enhancedData.zLevel,
                zLevelName = enhancedData.zLevelName
            };

            // Create enhanced marker data
            var marker = CreatePlayerMarker(
                enhancedData.latitude,
                enhancedData.longitude,
                enhancedData.zLevel,
                enhancedData.zLevelName,
                enhancedData.timestamp.ToString()
            );

            parsedGuess = new ParsedGuess(
                location,
                marker,
                true,
                $"Map clicked at: {enhancedData.latitude}, {enhancedData.longitude}, Level: {enhancedData.zLevelName}"
            );
            return true;
        }

        // Fallback to legacy data
        var clickData = JsonUtility.FromJson<MapClickData>(jsonData);
        if (clickData != null)
        {
            var location = new MapInteractionManager.LocationData
            {
                latitude = clickData.latitude,
                longitude = clickData.longitude,
                zLevel = currentZLevel,
                zLevelName = ZLevelFormatter.GetName(currentZLevel)
            };

            parsedGuess = new ParsedGuess(
                location,
                null,
                false,
                $"Map clicked at: {clickData.latitude}, {clickData.longitude} (legacy data)"
            );
            return true;
        }

        parsedGuess = default;
        return false;
    }

    public static bool TryParseSubmittedGuess(string jsonData, int currentZLevel, out ParsedGuess parsedGuess)
    {
        // Try to parse enhanced payload first
        var payload = JsonUtility.FromJson<MapInteractionManager.LocationPayload>(jsonData);
        if (payload != null && !string.IsNullOrEmpty(payload.zLevelName))
        {
            // Enhanced data with z-level
            var location = new MapInteractionManager.LocationData
            {
                latitude = payload.latitude,
                longitude = payload.longitude,
                zLevel = payload.zLevel,
                zLevelName = payload.zLevelName
            };

            // Create enhanced marker data for guess
            var marker = CreatePlayerMarker(
                payload.latitude,
                payload.longitude,
                payload.zLevel,
                payload.zLevelName,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
            );

            parsedGuess = new ParsedGuess(
                location,
                marker,
                true,
                $"Guess submitted at: latitude:{payload.latitude}, longitude:{payload.longitude}, zLevel: {payload.zLevelName}"
            );
            return true;
        }

        // Fallback to legacy data
        var guessData = JsonUtility.FromJson<MapClickData>(jsonData);
        if (guessData != null)
        {
            var location = new MapInteractionManager.LocationData
            {
                latitude = guessData.latitude,
                longitude = guessData.longitude,
                zLevel = currentZLevel,
                zLevelName = ZLevelFormatter.GetName(currentZLevel)
            };

            // Create basic marker data for legacy guess
            var marker = CreatePlayerMarker(
                guessData.latitude,
                guessData.longitude,
                currentZLevel,
                ZLevelFormatter.GetName(currentZLevel),
                guessData.timestamp.ToString()
            );

            parsedGuess = new ParsedGuess(
                location,
                marker,
                true,
                $"Guess submitted at: {guessData.latitude}, {guessData.longitude} (legacy data)"
            );
            return true;
        }

        parsedGuess = default;
        return false;
    }

    private static MapInteractionManager.MarkerData CreatePlayerMarker(
        float latitude,
        float longitude,
        int zLevel,
        string? zLevelName,
        string timestamp
    )
    {
        return new MapInteractionManager.MarkerData
        {
            id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
            lng = longitude,
            lat = latitude,
            zLevel = zLevel,
            zLevelName = zLevelName,
            timestamp = timestamp,
            options = new MapInteractionManager.MarkerOptions
            {
                imgUrl = "images/handthing.svg",
                imgScale = 1.7f,
                color = "white",
                size = 60,
                innerCircle = false,
                shape = "marker",
                zLevel = zLevel
            },
            markerType = "player"
        };
    }

    // These DTO fields are assigned by JsonUtility through reflection.
#pragma warning disable CS0649

    // Enhanced data structure for JSON parsing with z-level support
    [System.Serializable]
    private class EnhancedMapClickData
    {
        public float latitude;
        public float longitude;
        public int zLevel;
        public string? zLevelName;
        public long timestamp;
    }

    // Legacy data structure for backward compatibility
    [System.Serializable]
    private class MapClickData
    {
        public float latitude;
        public float longitude;
        public long timestamp;
    }

#pragma warning restore CS0649
}
