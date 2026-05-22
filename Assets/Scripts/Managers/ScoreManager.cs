using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private ScoreDataScriptableObject scoreData;

    public void ApplyScore(
        GameSessionData session,
        int score,
        int distance,
        int floorDiff,
        bool tooHigh)
    {
        session.CurrentScore += score;

        if (scoreData == null)
        {
            return;
        }

        scoreData.SetTotalScore(session.CurrentScore);
        scoreData.SetRoundScore(score);
        scoreData.SetDistanceScore(distance);
        scoreData.SetFloorData(floorDiff, tooHigh);
        scoreData.AddScore(score);
    }
}