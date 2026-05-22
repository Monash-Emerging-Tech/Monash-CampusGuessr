using System.Collections;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [SerializeField] private LocationManager locationManager;
    [SerializeField] private MapPackManager mapPackManager;
    [SerializeField] private PreloadManager preloadManager;
    [SerializeField] private ScoreDataScriptableObject scoreData;

    public IEnumerator StartRound(
        GameSessionData session,
        int totalRounds)
    {
        locationManager.SetCurrentMapPack(
            mapPackManager.ResolvedMapPackId);

        var currentMapPack = locationManager.GetCurrentMapPack();

        int locationId;

        if (session.QueuedNextLocationId.HasValue)
        {
            locationId = session.QueuedNextLocationId.Value;
            session.QueuedNextLocationId = null;
        }
        else
        {
            var usedIds = GameRoundHelper.GetUsedLocationIds(scoreData);

            if (!GameRoundHelper.TryPickRandomLocationId(
                locationManager,
                currentMapPack,
                usedIds,
                out locationId))
            {
                yield break;
            }
        }

        yield return StartCoroutine(
            locationManager.EnsureLocationMaterialLoaded(locationId));

        if (!locationManager.TryGetLocationById(locationId, out var location))
        {
            yield break;
        }

        locationManager.ApplyLocationToSkybox(location);

        scoreData.AddLocation(location);

        MapInteractionManager.Instance?.SetActualLocation(
            location.latitude,
            location.longitude,
            location.zLevel);

        MapInteractionManager.Instance?.ShowMap();
        MapInteractionManager.Instance?.SetWebGuessingState(true);
    }
}