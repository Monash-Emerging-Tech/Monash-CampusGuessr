using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 
/// Written by rohan0221
/// Last Modified: 23/04/2026
/// </summary>
public class MapButtonClick : MonoBehaviour
{
    public string targetScene;

    void OnMouseDown()
    {
        SceneManager.LoadScene(targetScene);
    }
}