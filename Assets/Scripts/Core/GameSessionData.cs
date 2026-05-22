[System.Serializable]
public class GameSessionData
{
    public int CurrentRound = 1;
    public int CurrentScore = 0;

    public int? QueuedNextLocationId = null;
    public int PreloadTriggeredForRound = -1;

    public void Reset()
    {
        CurrentRound = 1;
        CurrentScore = 0;
        QueuedNextLocationId = null;
        PreloadTriggeredForRound = -1;
    }
}