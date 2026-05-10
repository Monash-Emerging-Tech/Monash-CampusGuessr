using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/Score Data")]
public class ScoreDataScriptableObject : ScriptableObject
{
    [SerializeField] private int currentScore;
    [SerializeField] private int roundScore;
    [SerializeField] private int distanceScore;

    [SerializeField] private List<LocationManager.Location> prevLocations = new List<LocationManager.Location>();
    [SerializeField] private List<int> prevScores = new List<int>();



    public int CurrentScore => currentScore;

    public int RoundScore => roundScore;
    public int DistanceScore => distanceScore;

    public List<LocationManager.Location> PreviousLocations => prevLocations;

    public List<int> PreviousScores => prevScores;


    public void SetTotalScore(int value) => currentScore = value;

    public void SetRoundScore(int value) => roundScore = value;

    public void SetDistanceScore(int value) => distanceScore = value;

    public void ResetAll() {
        currentScore = 0;
        prevLocations.Clear();
        prevScores.Clear();
    } 


    public void AddLocation(LocationManager.Location location) {
        prevLocations.Add(location);
    }

    public void AddScore(int score)
    {
        prevScores.Add(score);
    }

    public string GetScoreData(string data) {

        switch (data)
        {
            case "total":
                return currentScore.ToString();
            case "round-score":
                return roundScore.ToString();
            case "round-distance":
                return distanceScore + "m";
            case "current-location":
                if (prevLocations.Count > 0)
                    return prevLocations[prevLocations.Count - 1].Name;
                else
                    return "N/A";
            default:
                return currentScore.ToString();
        }

    }

    public List<string> GetFormattedRoundBreakdown()
    {
        List<string> formattedRounds = new List<string>();
        
        for (int i = 0; i < prevLocations.Count && i < prevScores.Count; i++)
        {
            int roundNumber = i + 1;
            string locationName = prevLocations[i].Name;
            int score = prevScores[i];
            
            string formattedRound = $"{roundNumber}: {locationName} - {score}";
            formattedRounds.Add(formattedRound);
        }
        
        return formattedRounds;
    }

    /* Function for Calculating the Score based on the distance away
    // The Score is based on how close you are to the target, with different 'harshness' applied to certain distances
    // Within 3m is a Perfect Guess
    // Between 4m and 25m inclusive the score will drop from 495 to 450
    // Between 26m and 50m inclusive the score will drop from 447 to 350
     Everything above 700m will result in a score of 0

     Feel Free to play around with the values to make certain distance regions more harsh than others, this does not take into account z level

    */
    public static int CalculateScore(int distance)
    {

        switch (distance)
        {
            case <= 3:
                return 500;

            case <= 25:
                return CalculateInterval(distance, 4, 25, 495, 450);

            case <= 50:
                return CalculateInterval(distance, 26, 50, 447, 350); 

            case <= 250:
                return CalculateInterval(distance, 51, 250, 345, 200); 

            case <= 300:
                return CalculateInterval(distance, 251, 300, 198, 100); 

            case <= 500:
                return CalculateInterval(distance, 301, 500, 98, 10); 

            case <= 700:
                return CalculateInterval(distance, 501, 700, 10, 0); 

            default:
                return 0;
        }

        int CalculateInterval(int distance, int minDis, int maxDis, int maxScore, int minScore) {

            double gradient = (double)(minScore - maxScore) / (maxDis - minDis); // Negative gradient

            int score = (int)Math.Round(maxScore + gradient*(distance - minDis));

            return score;
        }

    }

}
public static class ScoreAnimationSettings
{
    public const float Duration = 0.8f;

    public static float Ease(float t)
    {
        return Mathf.SmoothStep(0, 1, t);
    }
}