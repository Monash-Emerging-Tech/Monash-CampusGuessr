using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Listens for final game results and submits leaderboard scores through Supabase.
///
/// Written by jwon0200
/// Last Modified: 07/09/2026
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    [Serializable]
    private struct MapLeaderboardSlug
    {
        public int mapPackId;
        public string mapSlug;
    }

    [SerializeField] private string submissionLeaderboardSlug = "monthly_score";

    [SerializeField]
    private MapLeaderboardSlug[] mapSlugs =
    {
        new() { mapPackId = 3, mapSlug = "clayton" },
        new() { mapPackId = 4, mapSlug = "college" }
    };

    [Header("Retry")]
    [SerializeField] private int maxSubmitAttempts = 2;
    [SerializeField] private float retryDelaySeconds = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    public static LeaderboardManager Instance { get; private set; }
    public static event Action SubmissionStarted;
    public static event Action<SubmitScoreResponse> SubmissionCompleted;

    // Stored so the breakdown scene can read the latest result even if it loads after the request finishes.
    public SubmitScoreResponse LastSubmitResponse { get; private set; }
    public Task<SubmitScoreResponse> CurrentSubmissionTask { get; private set; }
    public bool IsSubmitting { get; private set; }
    public bool HasAttemptedSubmission { get; private set; }
    public string LastError { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GameLogic.OnGameStarted += OnGameStarted;
        GameLogic.OnFinalScoreReady += OnFinalScoreReady;
    }

    private void OnDisable()
    {
        GameLogic.OnGameStarted -= OnGameStarted;
        GameLogic.OnFinalScoreReady -= OnFinalScoreReady;
    }

    private void OnGameStarted()
    {
        HasAttemptedSubmission = false;
        LastSubmitResponse = null;
        CurrentSubmissionTask = null;
        LastError = string.Empty;
    }

    private async void OnFinalScoreReady(GameResult gameResult)
    {
        if (gameResult == null)
        {
            return;
        }

        await SubmitFinalScoreAsync(gameResult);
    }

    public Task<SubmitScoreResponse> SubmitFinalScoreAsync(GameResult gameResult)
    {
        if (gameResult == null)
        {
            throw new ArgumentNullException(nameof(gameResult));
        }

        if (HasAttemptedSubmission || IsSubmitting)
        {
            return CurrentSubmissionTask ?? Task.FromResult(LastSubmitResponse);
        }

        LastError = string.Empty;
        LastSubmitResponse = null;
        HasAttemptedSubmission = true;
        IsSubmitting = true;

        CurrentSubmissionTask = SubmitFinalScoreInternalAsync(gameResult);
        SubmissionStarted?.Invoke();

        return CurrentSubmissionTask;
    }

    private async Task<SubmitScoreResponse> SubmitFinalScoreInternalAsync(GameResult gameResult)
    {
        try
        {
            SupabaseClient client = ResolveSupabaseClient();

            if (!TryResolveMapSlug(gameResult.MapPackId, out string mapSlug))
            {
                throw new InvalidOperationException("Could not resolve leaderboard map slug.");
            }

            SubmitScoreResponse response = await SubmitScoreWithRetryAsync(
                client,
                mapSlug,
                submissionLeaderboardSlug,
                gameResult.FinalScore,
                gameResult.TimeSeconds);

            LastSubmitResponse = response;
            LastError = response == null || response.accepted
                ? string.Empty
                : response.error ?? "Score was not accepted.";

            LogDebug($"Submit complete. accepted={response?.accepted}, top50={response?.isTop50}, rank={response?.rank}, scoreEntryId={response?.scoreEntryId}, error={response?.error}");
            return response;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            LogError($"Submit failed: {ex.Message}");
            return null;
        }
        finally
        {
            IsSubmitting = false;
            SubmissionCompleted?.Invoke(LastSubmitResponse);
            CurrentSubmissionTask = null;
        }
    }

    public async Task<LeaderboardTopScoreRow[]> GetTopScoresAsync(
        string mapSlug,
        string resolvedLeaderboardSlug,
        int maxRows)
    {
        SupabaseClient client = ResolveSupabaseClient();
        string targetLeaderboardSlug = string.IsNullOrWhiteSpace(resolvedLeaderboardSlug)
            ? submissionLeaderboardSlug
            : resolvedLeaderboardSlug;

        return await client.GetTopScoresAsync(
            mapSlug,
            targetLeaderboardSlug,
            string.Empty,
            maxRows);
    }

    public async Task<UpdateScoreNameResponse> UpdateScoreNameAsync(
        long scoreEntryId,
        string displayName)
    {
        SupabaseClient client = ResolveSupabaseClient();
        return await client.UpdateScoreNameAsync(scoreEntryId, displayName);
    }

    public bool TryResolveMapSlug(int mapPackId, out string mapSlug)
    {
        foreach (MapLeaderboardSlug entry in mapSlugs)
        {
            if (entry.mapPackId == mapPackId && !string.IsNullOrWhiteSpace(entry.mapSlug))
            {
                mapSlug = entry.mapSlug;
                return true;
            }
        }

        mapSlug = string.Empty;
        return false;
    }

    private async Task<SubmitScoreResponse> SubmitScoreWithRetryAsync(
        SupabaseClient client,
        string mapSlug,
        string resolvedLeaderboardSlug,
        int score,
        float timeSeconds)
    {
        int attempts = Mathf.Max(1, maxSubmitAttempts);
        Exception lastException = null;

        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                return await client.SubmitScoreAsync(
                    mapSlug,
                    resolvedLeaderboardSlug,
                    score,
                    timeSeconds);
            }
            catch (Exception ex) when (ShouldRetrySubmit(ex, attempt, attempts))
            {
                lastException = ex;
                LogDebug($"Submit attempt {attempt} failed, retrying: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, retryDelaySeconds)));
            }
            catch
            {
                throw;
            }
        }

        throw lastException ?? new InvalidOperationException("Score submit failed.");
    }

    private static bool ShouldRetrySubmit(Exception exception, int attempt, int maxAttempts)
    {
        if (attempt >= maxAttempts)
        {
            return false;
        }

        if (exception is not SupabaseRequestException requestException)
        {
            return false;
        }

        return requestException.StatusCode == 0 ||
               requestException.StatusCode == 408 ||
               requestException.StatusCode == 429 ||
               requestException.StatusCode >= 500;
    }

    private SupabaseClient ResolveSupabaseClient()
    {
        SupabaseClient client = SupabaseClient.Instance;

        if (client == null)
        {
            throw new InvalidOperationException("SupabaseClient is missing from the scene.");
        }

        return client;
    }

    #region Debug Logging

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[LeaderboardManager] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[LeaderboardManager] {message}");
    }

    #endregion
}
