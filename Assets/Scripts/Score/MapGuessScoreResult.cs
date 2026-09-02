/// <summary>
/// Score calculation result for a submitted map guess.
/// </summary>
public readonly struct MapGuessScoreResult
{
    public MapGuessScoreResult(
        int score,
        int distance,
        int floorDiff,
        bool tooHigh,
        float exactDistance,
        bool hasZLevelPenalty,
        int preZScore,
        float zModifier
    )
    {
        Score = score;
        Distance = distance;
        FloorDiff = floorDiff;
        TooHigh = tooHigh;
        ExactDistance = exactDistance;
        HasZLevelPenalty = hasZLevelPenalty;
        PreZScore = preZScore;
        ZModifier = zModifier;
    }

    public int Score { get; }
    public int Distance { get; }
    public int FloorDiff { get; }
    public bool TooHigh { get; }
    public float ExactDistance { get; }
    public bool HasZLevelPenalty { get; }
    public int PreZScore { get; }
    public float ZModifier { get; }
}
