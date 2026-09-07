using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Handles Supabase auth and leaderboard requests for the Unity client.
///
/// Written by jwon0200
/// Last Modified: 27/08/2026
/// </summary>
public class SupabaseClient : MonoBehaviour
{
    [SerializeField] private SupabaseConfig config;
    [SerializeField] private bool enableDebugLogs = true;

    public static SupabaseClient Instance { get; private set; }

    public string UserId { get; private set; }
    public bool IsSignedIn => !string.IsNullOrWhiteSpace(accessToken);

    private string accessToken;
    // Reused while sign in is already running so multiple callers do not create multiple anonymous users.
    private Task signInTask;

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

    private async void Start()
    {
        try
        {
            await EnsureSignedInAsync();
        }
        catch (Exception ex)
        {
            LogError($"Anonymous sign-in failed: {ex.Message}");
        }
    }

    public async Task EnsureSignedInAsync()
    {
        if (IsSignedIn)
        {
            return;
        }

        signInTask ??= SignInAnonymouslyAsync();

        try
        {
            await signInTask;
        }
        finally
        {
            signInTask = null;
        }
    }

    /// <summary>
    /// Sends the final game score to the submit-score Edge Function.
    /// The backend decides the leaderboard period and does final validation.
    /// </summary>
    public async Task<SubmitScoreResponse> SubmitScoreAsync(
        string mapSlug,
        string leaderboardSlug,
        int score,
        float timeSeconds)
    {
        ValidateConfig();
        await EnsureSignedInAsync();

        SubmitScoreRequest payload = new()
        {
            mapSlug = mapSlug,
            leaderboardSlug = leaderboardSlug,
            score = score,
            timeSeconds = timeSeconds
        };

        string url = config.GetFunctionUrl("submit-score");
        string responseJson = await PostJsonAsync(url, JsonUtility.ToJson(payload), true);
        return JsonUtility.FromJson<SubmitScoreResponse>(responseJson);
    }

    /// <summary>
    /// Updates the display name for a submitted score owned by the anonymous user.
    /// </summary>
    public async Task<UpdateScoreNameResponse> UpdateScoreNameAsync(long scoreEntryId, string displayName)
    {
        ValidateConfig();
        await EnsureSignedInAsync();

        UpdateScoreNameRequest payload = new()
        {
            scoreEntryId = scoreEntryId,
            displayName = displayName
        };

        string url = config.GetFunctionUrl("update-score-name");
        string responseJson = await PostJsonAsync(url, JsonUtility.ToJson(payload), true);
        return JsonUtility.FromJson<UpdateScoreNameResponse>(responseJson);
    }

    /// <summary>
    /// Loads top scores from Supabase.
    /// Empty periodKey means the server's current period is used.
    /// </summary>
    public async Task<LeaderboardTopScoreRow[]> GetTopScoresAsync(
        string mapSlug,
        string leaderboardSlug,
        string periodKey = "",
        int limit = 50)
    {
        ValidateConfig();

        bool useCurrentPeriod = string.IsNullOrWhiteSpace(periodKey);
        string viewName = useCurrentPeriod
            ? "leaderboard_current_top_scores"
            : "leaderboard_top_scores";

        string url =
            $"{config.SupabaseUrl}/rest/v1/{viewName}" +
            "?select=id,display_name,score,time_seconds,created_at,rank" +
            $"&map_slug=eq.{EscapeQueryValue(mapSlug)}" +
            $"&leaderboard_slug=eq.{EscapeQueryValue(leaderboardSlug)}";

        if (!useCurrentPeriod)
        {
            url += $"&period_key=eq.{EscapeQueryValue(periodKey)}";
        }

        url += $"&order=rank.asc&limit={Mathf.Clamp(limit, 1, 50)}";

        string responseJson = await GetJsonAsync(url);
        return JsonArrayUtility.FromJson<LeaderboardTopScoreRow>(responseJson);
    }

    private async Task<string> GetJsonAsync(string url)
    {
        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("apikey", config.PublishableKey);

        return await SendAsync(request);
    }

    private async Task SignInAnonymouslyAsync()
    {
        ValidateConfig();

        string url = $"{config.SupabaseUrl}/auth/v1/signup";
        string responseJson = await PostJsonAsync(url, "{\"data\":{}}", false);
        SupabaseAuthResponse response = JsonUtility.FromJson<SupabaseAuthResponse>(responseJson);

        if (response == null || string.IsNullOrWhiteSpace(response.access_token))
        {
            throw new InvalidOperationException("Supabase did not return an anonymous access token.");
        }

        accessToken = response.access_token;
        UserId = response.user?.id;

        LogDebug($"Anonymous sign-in succeeded. User ID: {UserId}");
    }

    private async Task<string> PostJsonAsync(string url, string json, bool includeAuth)
    {
        using UnityWebRequest request = new(url, "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("apikey", config.PublishableKey);

        if (includeAuth)
        {
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        }

        return await SendAsync(request);
    }

    private static async Task<string> SendAsync(UnityWebRequest request)
    {
        request.timeout = 10;

        await request.SendWebRequest().AsTask();

        if (request.result != UnityWebRequest.Result.Success)
        {
            string responseBody = request.downloadHandler?.text ?? string.Empty;
            throw new SupabaseRequestException(request.responseCode, request.error, responseBody);
        }

        return request.downloadHandler.text;
    }

    private void ValidateConfig()
    {
        if (config == null || !config.IsConfigured)
        {
            throw new InvalidOperationException("SupabaseConfig is missing or incomplete.");
        }
    }

    private static string EscapeQueryValue(string value)
    {
        return Uri.EscapeDataString(value?.Trim() ?? string.Empty);
    }

    #region Debug Logging

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[SupabaseClient] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SupabaseClient] {message}");
    }

    #endregion
}

[Serializable]
public class SupabaseAuthResponse
{
    public string access_token;
    public SupabaseUser user;
}

[Serializable]
public class SupabaseUser
{
    public string id;
}

[Serializable]
public class SubmitScoreRequest
{
    public string mapSlug;
    public string leaderboardSlug;
    public int score;
    public float timeSeconds;
}

[Serializable]
public class SubmitScoreResponse
{
    public bool accepted;
    public bool isTop50;
    public int rank;
    public long scoreEntryId;
    public int score;
    public float timeSeconds;
    public string periodKey;
    public string error;
}

[Serializable]
public class UpdateScoreNameRequest
{
    public long scoreEntryId;
    public string displayName;
}

[Serializable]
public class UpdateScoreNameResponse
{
    public bool accepted;
    public long scoreEntryId;
    public string displayName;
    public string error;
}

[Serializable]
public class LeaderboardTopScoreRow
{
    public long id;
    public string display_name;
    public int score;
    public float time_seconds;
    public string created_at;
    public int rank;
}

public class SupabaseRequestException : Exception
{
    public long StatusCode { get; }
    public string ResponseBody { get; }

    public SupabaseRequestException(long statusCode, string error, string responseBody)
        : base($"Supabase request failed ({statusCode}): {error}. {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}

public static class UnityWebRequestAsyncOperationExtensions
{
    public static Task AsTask(this UnityWebRequestAsyncOperation operation)
    {
        TaskCompletionSource<bool> completion = new();
        operation.completed += _ => completion.TrySetResult(true);
        return completion.Task;
    }
}

public static class JsonArrayUtility
{
    public static T[] FromJson<T>(string json)
    {
        string wrappedJson = $"{{\"items\":{json}}}";
        JsonArrayWrapper<T> wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(wrappedJson);
        return wrapper?.items ?? Array.Empty<T>();
    }

    [Serializable]
    private class JsonArrayWrapper<T>
    {
        public T[] items = Array.Empty<T>();
    }
}
