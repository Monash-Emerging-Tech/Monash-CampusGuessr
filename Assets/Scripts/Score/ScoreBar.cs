using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>  
/// @author: Salma
/// @modified: 2026-05-01
/// 
public class ScoreBar : MonoBehaviour
{
    public int maximum;
    public Image mask;

    [SerializeField] private ScoreDataScriptableObject scoreData;

    private Coroutine animRoutine;

    private void OnEnable()
    {
        AnimateBar(scoreData.CurrentScore);
    }
    private void AnimateBar(int newScore)
    {
        //Reset to 0 before animating to new score
        mask.fillAmount = 0;

        if (animRoutine != null)
            StopCoroutine(animRoutine);

        animRoutine = StartCoroutine(AnimateFill(newScore));
    }

    private IEnumerator AnimateFill(int targetScore)
    {
        float duration = ScoreAnimationSettings.Duration;
        float time = 0f;

        float start = mask.fillAmount;
        float target = (float)targetScore / maximum;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            t = ScoreAnimationSettings.Ease(t);

            mask.fillAmount = Mathf.Lerp(start, target, t);

            yield return null;
        }

        mask.fillAmount = target;
    }
}