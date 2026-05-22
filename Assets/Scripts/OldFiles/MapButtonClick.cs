using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Written by rohan0221
/// Last Modified: 23/04/2026
/// </summary>
public class MapButtonClick : MonoBehaviour
{
    public string targetScene;
    public string mapPackName;
    public float mapCenterLat;
    public float mapCenterLng;
    public int mapZoom = 16;

    void OnMouseDown()
    {
        if (!string.IsNullOrEmpty(mapPackName) && GameLogic.Instance != null)
        {
            GameLogic.Instance.SetMapPackByName(mapPackName);
            GameLogic.Instance.SetPendingMapCenter(mapCenterLat, mapCenterLng, mapZoom);
        }
        
        SceneManager.LoadScene(targetScene);
    }
    public void OnButtonClick()
    {
        OnMouseDown();
    }
}