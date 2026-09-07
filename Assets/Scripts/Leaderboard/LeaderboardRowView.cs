using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays one leaderboard row using scene designed UI.
///
/// Written by jwon0200
/// Last Modified: 04/09/2026
/// </summary>
public class LeaderboardRowView : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private GameObject currentScoreMarker;

    [Header("Colours")]
    [SerializeField] private Color normalBackgroundColor = new(1f, 1f, 1f, 0.08f);
    [SerializeField] private Color currentScoreBackgroundColor = new(0.86f, 0.11f, 0.24f, 0.9f);
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color currentScoreTextColor = Color.white;

    public void SetData(int rank, LeaderboardTopScoreRow row, bool isCurrentScore)
    {
        string displayName = string.IsNullOrWhiteSpace(row.display_name)
            ? "Anonymous Player"
            : row.display_name;

        SetText(rankText, rank.ToString());
        SetText(nameText, displayName);
        SetText(scoreText, $"{row.score} pts");
        SetText(timeText, FormatTime(row.time_seconds));
        ApplyCurrentScoreStyle(isCurrentScore);
    }

    private void ApplyCurrentScoreStyle(bool isCurrentScore)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = isCurrentScore ? currentScoreBackgroundColor : normalBackgroundColor;
        }

        Color textColor = isCurrentScore ? currentScoreTextColor : normalTextColor;
        SetTextColor(rankText, textColor);
        SetTextColor(nameText, textColor);
        SetTextColor(scoreText, textColor);
        SetTextColor(timeText, textColor);

        if (currentScoreMarker != null)
        {
            currentScoreMarker.SetActive(isCurrentScore);
        }
    }

    private static string FormatTime(float timeSeconds)
    {
        return timeSeconds > 0f ? $"{timeSeconds:0.0}s" : "--";
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private static void SetTextColor(TextMeshProUGUI text, Color color)
    {
        if (text != null)
        {
            text.color = color;
        }
    }
}
