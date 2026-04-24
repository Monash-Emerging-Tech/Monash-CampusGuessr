using UnityEngine;
/// <summary>
/// 
/// Written by rohan0221
/// Last Modified: 23/04/2026
/// Modified by : salma
/// </summary>
public class MapButtonClick : MonoBehaviour
{
    void OnMouseDown()
    {
        CoroutineRunner.Instance.StartCoroutine(SceneLoader.LoadSceneAsync(Constants.Scenes.MAIN_MENU));
    }
}