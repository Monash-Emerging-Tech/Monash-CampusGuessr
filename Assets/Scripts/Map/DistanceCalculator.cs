using UnityEngine;

public static class DistanceCalculator
{
    /// <summary>
    /// Calculates distance between two coordinates using simple approximation
    /// Accurate enough for campus distances (under 5km)
    /// </summary>
    /// <param name="coord1">First location data</param>
    /// <param name="coord2">Second location data</param>
    /// <returns>Distance in meters</returns>
    public static float CalculateMeters(MapInteractionManager.LocationData coord1, MapInteractionManager.LocationData coord2)
    {
        // Convert degrees to approximate meters
        // 1 degree latitude ~ 111,000 meters
        // 1 degree longitude ~ 111,000 * cos(latitude) meters
        float latDiff = (coord2.latitude - coord1.latitude) * 111000f;
        float lngDiff = (coord2.longitude - coord1.longitude) * 111000f * Mathf.Cos(coord1.latitude * Mathf.Deg2Rad);

        return Mathf.Sqrt(latDiff * latDiff + lngDiff * lngDiff);
    }
}
