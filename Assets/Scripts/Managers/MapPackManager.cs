using UnityEngine;

public class MapPackManager : MonoBehaviour
{
    [SerializeField] private LocationManager locationManager;

    public int ResolvedMapPackId { get; private set; } = 2;

    public float PendingMapCenterLat { get; private set; } = -37.9106f;
    public float PendingMapCenterLng { get; private set; } = 145.1361f;
    public int PendingMapZoom { get; private set; } = 16;
    public int PendingCampusId { get; private set; } = 159;

    public bool ResolveMapPack(string mapPackName)
    {
        if (locationManager == null)
        {
            return false;
        }

        int id = locationManager.GetMapPackIdByName(mapPackName);

        if (id == -1)
        {
            return false;
        }

        ResolvedMapPackId = id;

        ApplyMapPackSettings(id);

        return true;
    }

    private void ApplyMapPackSettings(int id)
    {
        var dict = locationManager.GetMapPackDict();

        if (dict == null || !dict.ContainsKey(id))
        {
            return;
        }

        PendingCampusId = dict[id].campusId != 0
            ? dict[id].campusId
            : 159;

        bool isCollege = PendingCampusId == 413;

        PendingMapCenterLat = isCollege
            ? -37.820049f
            : -37.9106f;

        PendingMapCenterLng = isCollege
            ? 144.949381f
            : 145.1361f;

        PendingMapZoom = isCollege ? 18 : 16;
    }
}