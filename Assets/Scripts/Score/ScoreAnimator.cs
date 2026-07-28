using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreAnimator : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] textFields;
    [SerializeField] private ScoreDataScriptableObject scoreData;

    private Coroutine animRoutine;

    private void Awake()
    {
        if (textFields == null || textFields.Length == 0)
            textFields = GetComponentsInChildren<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        HandleScoreChanged(scoreData.CurrentScore);
    }
    private void HandleScoreChanged(int newValue)
    {
        if (animRoutine != null)
            StopCoroutine(animRoutine);

        animRoutine = StartCoroutine(AnimateText(newValue));
    }

    private IEnumerator AnimateText(int to)
    {
        int from = Mathf.Max(0, Mathf.RoundToInt(to * 0.93f));
        float duration = ScoreAnimationSettings.Duration;
        float time = 0f;

        AudioManager.Instance?.PlayScoreSFX();

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            t = ScoreAnimationSettings.Ease(t);

            int value = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
            SetText(value);

            yield return null;
        }

        AudioManager.Instance?.StopScoreSFX(0.7f);
        SetText(to);
    }

    private void SetText(int value)
    {
        string formatted = value.ToString();

        foreach (var tmp in textFields)
        {
            if (tmp != null)
                tmp.text = formatted;
        }
    }


    private void Start()
    {
       Test();
    }
    public void Test()
    {
       HandleScoreChanged(1500);
    }
}
