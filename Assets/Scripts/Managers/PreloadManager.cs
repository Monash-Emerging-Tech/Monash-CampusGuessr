using System.Collections;
using UnityEngine;

public class PreloadManager : MonoBehaviour
{
    [SerializeField] private LocationManager locationManager;
    [SerializeField] private ScoreDataScriptableObject scoreData;

    private Coroutine preloadCoroutine;

    public void QueueNextLocation(
        GameSessionData session,
        LocationManager.MapPack mapPack,
        int? currentLocationId)
    {
        var excluded = GameRoundHelper.GetUsedLocationIds(scoreData);

        if (currentLocationId.HasValue)
        {
            excluded.Add(currentLocationId.Value);
        }

        if (!GameRoundHelper.TryPickRandomLocationId(
            locationManager,
            mapPack,
            excluded,
            out int nextLocationId))
        {
            return;
        }

        session.QueuedNextLocationId = nextLocationId;

        if (!locationManager.IsLocationMaterialLoaded(nextLocationId))
        {
            preloadCoroutine = StartCoroutine(
                locationManager.EnsureLocationMaterialLoaded(nextLocationId));
        }
    }
}