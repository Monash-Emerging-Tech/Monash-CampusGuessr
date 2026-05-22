using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GameRoundHelper
{
    public static HashSet<int> GetUsedLocationIds(ScoreDataScriptableObject scoreData)
    {
        if (scoreData == null || scoreData.PreviousLocations == null)
        {
            return new HashSet<int>();
        }

        return scoreData.PreviousLocations
            .Select(loc => loc.ID)
            .ToHashSet();
    }

    public static bool TryPickRandomLocationId(
        LocationManager locationManager,
        LocationManager.MapPack mapPack,
        HashSet<int> excludedIds,
        out int locationId)
    {
        locationId = -1;

        var locations = locationManager.GetLocationsFromMapPack(mapPack);

        if (locations.Count == 0)
        {
            return false;
        }

        var candidates = excludedIds == null || excludedIds.Count == 0
            ? locations
            : locations.Where(loc => !excludedIds.Contains(loc.ID)).ToList();

        if (candidates.Count == 0)
        {
            candidates = locations;
        }

        int index = Random.Range(0, candidates.Count);

        locationId = candidates[index].ID;

        return true;
    }
}