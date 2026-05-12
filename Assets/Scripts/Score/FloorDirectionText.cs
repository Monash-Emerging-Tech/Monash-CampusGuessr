using UnityEngine;
using TMPro;

public class FloorDirectionText : MonoBehaviour
{
    [SerializeField] private ScoreDataScriptableObject scoreData;
    [SerializeField] private TextMeshProUGUI textField;

    private void Awake()
    {
        if (textField == null)
            textField = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        UpdateText();
    }

    public void UpdateText()
    {
        if (scoreData != null && textField != null)
            textField.text = scoreData.GetScoreData("floor-direction");
    }
}