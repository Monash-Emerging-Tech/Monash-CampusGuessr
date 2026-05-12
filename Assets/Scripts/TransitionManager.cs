using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [SerializeField] private RawImage irisImage;
    [SerializeField] private float transitionDuration = 0.6f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        SetProgress(0f); // Start fully open
    }

    public IEnumerator TransitionToScene(string sceneName)
    {
        yield return StartCoroutine(SetProgressCoroutine(0f, 1f)); // Close iris
        SceneManager.LoadScene(sceneName);
    }

    public IEnumerator OpenIris()
    {
        yield return StartCoroutine(SetProgressCoroutine(1f, 0f)); // Open iris
    }

    private void SetProgress(float value)
    {
        irisImage.material.SetFloat("_Progress", value);
    }

    private IEnumerator SetProgressCoroutine(float from, float to)
    {
        float elapsed = 0f;
        SetProgress(from);
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            SetProgress(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration)));
            yield return null;
        }
        SetProgress(to);
    }
}