using UnityEngine;

/// <summary>
/// Bridge for UI buttons that need to call the persistent GameLogic singleton.
/// Assign this component (or its GameObject) to the Start button's onClick in the Inspector,
/// and choose LoadGame() as the method. The button will then call GameLogic.Instance.LoadGame.
/// </summary>
public class MenuSceneBridge : MonoBehaviour
{
    public void LoadGame()
    {
        if (GameLogic.Instance != null)
            GameLogic.Instance.LoadGame();
    }
}
