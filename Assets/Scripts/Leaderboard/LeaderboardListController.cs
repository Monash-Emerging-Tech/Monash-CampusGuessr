using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Written by jwon0200
/// Last Modified: 04/09/2026
/// </summary>
public class LeaderboardListController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LeaderboardManager leaderboardManager;

    [Header("Leaderboard")]
    [SerializeField] private string mapSlug = "clayton";
    [SerializeField] private string leaderboardSlug = "monthly_score";
    [SerializeField] private int maxRows = 50;
    [SerializeField] private bool waitForScoreSubmission = true;

    [Header("UI References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform rowsParent;
    [SerializeField] private LeaderboardRowView rowPrefab;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private bool shouldLoadAfterSubmission = false;
    private int loadVersion = 0;

    private void Awake()
    {
        ResolveReferences();
        ConfigureScrollRect();
        SyncMapSlugFromGameLogic();
    }

    private void OnEnable()
    {
        GameLogic.OnMapPackChanged += OnMapPackChanged;
        LeaderboardManager.SubmissionStarted += OnSubmissionStarted;
        LeaderboardManager.SubmissionCompleted += OnSubmissionCompleted;
    }

    private void OnDisable()
    {
        GameLogic.OnMapPackChanged -= OnMapPackChanged;
        LeaderboardManager.SubmissionStarted -= OnSubmissionStarted;
        LeaderboardManager.SubmissionCompleted -= OnSubmissionCompleted;
    }

    private async void Start()
    {
        SyncMapSlugFromGameLogic();
        await WaitForScoreSubmissionAsync();
        await LoadLeaderboardAsync();
    }

    public async Task LoadLeaderboardAsync()
    {
        ResolveReferences();

        int requestVersion = ++loadVersion;

        ClearRows();
        SetStatusText("Loading leaderboard...");

        try
        {
            LeaderboardTopScoreRow[] rows = await ResolveLeaderboardManager().GetTopScoresAsync(
                mapSlug,
                leaderboardSlug,
                maxRows);

            if (requestVersion != loadVersion)
            {
                return;
            }

            PopulateRows(rows);
            ResetScrollPosition();
            SetStatusText(rows.Length == 0 ? "No scores yet." : string.Empty);
            LogDebug($"Loaded {rows.Length} leaderboard rows.");
        }
        catch (Exception ex)
        {
            if (requestVersion != loadVersion)
            {
                return;
            }

            SetStatusText("Could not load leaderboard.");
            LogError($"Load failed: {ex.Message}");
        }
    }

    public async void Refresh()
    {
        await LoadLeaderboardAsync();
    }

    private async void OnMapPackChanged(string mapPackName)
    {
        if (!SyncMapSlugFromGameLogic())
        {
            return;
        }

        await LoadLeaderboardAsync();
    }

    private void OnSubmissionStarted()
    {
        if (!waitForScoreSubmission)
        {
            return;
        }

        shouldLoadAfterSubmission = true;
        loadVersion++;
        ClearRows();
        SetStatusText("Submitting score...");
    }

    private async void OnSubmissionCompleted(SubmitScoreResponse response)
    {
        if (!waitForScoreSubmission || !shouldLoadAfterSubmission)
        {
            return;
        }

        shouldLoadAfterSubmission = false;
        await LoadLeaderboardAsync();
    }

    private void PopulateRows(LeaderboardTopScoreRow[] rows)
    {
        if (rowsParent == null || rowPrefab == null)
        {
            LogError("Rows parent or row prefab is missing.");
            return;
        }

        long currentScoreEntryId = leaderboardManager?.LastSubmitResponse?.scoreEntryId ?? 0;

        for (int i = 0; i < rows.Length; i++)
        {
            LeaderboardTopScoreRow row = rows[i];
            LeaderboardRowView rowView = Instantiate(rowPrefab, rowsParent);
            bool isCurrentScore = currentScoreEntryId > 0 && row.id == currentScoreEntryId;

            rowView.gameObject.SetActive(true);
            int rank = row.rank > 0 ? row.rank : i + 1;
            rowView.SetData(rank, row, isCurrentScore);
        }
    }

    private void ClearRows()
    {
        if (rowsParent == null)
        {
            return;
        }

        for (int i = rowsParent.childCount - 1; i >= 0; i--)
        {
            Transform child = rowsParent.GetChild(i);
            if (rowPrefab != null && child == rowPrefab.transform)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void ResolveReferences()
    {
        if (leaderboardManager == null)
        {
            leaderboardManager = LeaderboardManager.Instance;
        }

        if (scrollRect == null && rowsParent != null)
        {
            scrollRect = rowsParent.GetComponentInParent<ScrollRect>();
        }
    }

    private LeaderboardManager ResolveLeaderboardManager()
    {
        if (leaderboardManager == null)
        {
            leaderboardManager = LeaderboardManager.Instance;
        }

        if (leaderboardManager == null)
        {
            throw new InvalidOperationException("LeaderboardManager is missing from the scene.");
        }

        return leaderboardManager;
    }

    private void ConfigureScrollRect()
    {
        if (scrollRect == null)
        {
            return;
        }

        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = false;
        scrollRect.scrollSensitivity = 14f;
    }

    private void ResetScrollPosition()
    {
        if (scrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private bool SyncMapSlugFromGameLogic()
    {
        if (GameLogic.Instance == null)
        {
            return false;
        }

        ResolveReferences();
        if (leaderboardManager == null ||
            !leaderboardManager.TryResolveMapSlug(GameLogic.Instance.GetMapPackId(), out string selectedMapSlug))
        {
            return false;
        }

        mapSlug = selectedMapSlug;
        return true;
    }

    private async Task WaitForScoreSubmissionAsync()
    {
        if (!waitForScoreSubmission)
        {
            return;
        }

        ResolveReferences();

        Task<SubmitScoreResponse> submissionTask = leaderboardManager?.CurrentSubmissionTask;
        if (submissionTask == null)
        {
            return;
        }

        shouldLoadAfterSubmission = false;
        SetStatusText("Submitting score...");
        await submissionTask;
    }

    private void SetStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    #region Debug Logging

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[LeaderboardListController] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[LeaderboardListController] {message}");
    }

    #endregion
}
