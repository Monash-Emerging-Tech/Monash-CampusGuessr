using UnityEngine;
using TMPro;

/***
 * 
 * FancyText Controller, Responsible for updating the white, red and blue text components
 * 
 * Written by aleu0007
 * Last Modified: 1/08/2025
 * 
 */
public class FancyTextController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] textFields;
    // [SerializeField] private string editorText = "Default Text";
    [SerializeField] private ScoreDataScriptableObject scoreData;

    [SerializeField] private string data;

    private void OnValidate()
    {
        // This runs in the editor when values change
        UpdateTextFields();
    }

    public void Awake()
    {


        // Automatically find text fields if not assigned
        if (textFields == null || textFields.Length == 0)
        {
            textFields = GetComponentsInChildren<TextMeshProUGUI>();
        }

        // Update text fields at runtime
        UpdateTextFields();
    }

    [ContextMenu("Update Texts")]
    private void UpdateTextFields()
    {
        // Ensure we have text fields
        if (textFields == null || textFields.Length == 0)
        {
            textFields = GetComponentsInChildren<TextMeshProUGUI>();
        }

        //setting the editor text to the score from the scriptable object
        string editorText = (scoreData != null) ? scoreData.GetScoreData(data).ToString() : "0";

        // Update all text fields
        foreach (var tmp in textFields)
        {
            if (tmp != null)
                tmp.text = editorText;
        }
    }
}
