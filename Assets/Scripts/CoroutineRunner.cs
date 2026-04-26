using UnityEngine;

/// <summary>
/// Runs coroutines on an always-active GameObject. Use when a coroutine must run
/// even if the caller's GameObject may be inactive (e.g. LocationManager loading).
/// </summary>
public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance;

    public static CoroutineRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("CoroutineRunner");
                _instance = go.AddComponent<CoroutineRunner>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
}
