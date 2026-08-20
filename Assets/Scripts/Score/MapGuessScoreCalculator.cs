using UnityEngine;

/// <summary>
/// Calculates the score for a submitted map guess.
/// </summary>
public static class MapGuessScoreCalculator
{
    /// <summary>
    /// Calculates score based on distance between guess and actual location
    /// </summary>
    /// <param name="actual">Actual location data</param>
    /// <param name="guess">Guess location data</param>
    /// <param name="distanceScale">Distance multiplier for the active map pack</param>
    /// <param name="zLevelWeight">Z-level penalty weight for the active map pack</param>
    /// <returns>Score from 0 to maxScore</returns>
    public static MapGuessScoreResult Calculate(
        MapInteractionManager.LocationData actual,
        MapInteractionManager.LocationData guess,
        float distanceScale,
        float zLevelWeight
    )
    {
        // New Scoring Method (considers Z-levels AND distance scale) 18/05/2026
        float distance = DistanceCalculator.CalculateMeters(actual, guess);
        int score = ScoreDataScriptableObject.CalculateScore((int)(distance * distanceScale));

        // Apply z-level penalty
        int zLevelDiff = Mathf.Abs(actual.zLevel - guess.zLevel);
        int preZScore = score;
        float zModifier = 1f;

        if (zLevelDiff > 0)
        {
            float zPenalty = Mathf.Min(zLevelDiff * 0.25f * zLevelWeight, 1f);
            zModifier = 1f - zPenalty;
            score = Mathf.RoundToInt(score * zModifier);
        }

        bool tooHigh = guess.zLevel > actual.zLevel;

        return new MapGuessScoreResult(
            score,
            (int)distance,
            zLevelDiff,
            tooHigh,
            distance,
            zLevelDiff > 0,
            preZScore,
            zModifier
        );
    }
}
