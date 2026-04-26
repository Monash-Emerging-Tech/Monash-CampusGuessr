using UnityEngine;
using TMPro;
using System.Collections;

public class LoadingDots : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI loadingText;

    private IEnumerator AnimateDots()
    {
        string baseText = "Loading";
        int dotCount = 0;

        while (true)
        {
            dotCount = (dotCount + 1) % 4;
            loadingText.text = baseText + new string('.', dotCount);
            yield return new WaitForSeconds(0.4f);
        }
    }

    void OnEnable()
    {
        StartCoroutine(AnimateDots());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }
}