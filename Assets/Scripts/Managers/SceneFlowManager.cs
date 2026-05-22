using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    public void LoadGameScene()
    {
        SceneManager.LoadScene(SceneNames.GAME_SCENE);
    }

    public void LoadMapSelection()
    {
        SceneManager.LoadScene(SceneNames.MAP_SELECTION);
    }

    public void LoadBreakdownScene()
    {
        SceneManager.LoadScene(SceneNames.BREAKDOWN_SCENE);
    }
}