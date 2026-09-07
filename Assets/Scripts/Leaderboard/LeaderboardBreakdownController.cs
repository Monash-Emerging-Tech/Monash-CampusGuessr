using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls leaderboard UI in the breakdown scene.
///
/// Written by jwon0200
/// Last Modified: 27/08/2026
/// </summary>
public class LeaderboardBreakdownController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LeaderboardManager leaderboardManager;

    [Header("UI References")]
    [SerializeField] private GameObject loadingRoot;
    [SerializeField] private GameObject namePromptRoot;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TMP_InputField displayNameInput;
    [SerializeField] private Button submitNameButton;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private bool isUpdatingName = false;

    private void Awake()
    {
        ResolveReferences();
        SetNamePromptVisible(false);
        SetLoadingVisible(true);
    }

    private void OnEnable()
    {
        LeaderboardManager.SubmissionCompleted += OnSubmissionCompleted;

        if (submitNameButton != null)
        {
            submitNameButton.onClick.AddListener(OnSubmitNameClicked);
        }
    }

    private async void Start()
    {
        await WaitForSubmitResultAsync();
    }

    private void OnDisable()
    {
        LeaderboardManager.SubmissionCompleted -= OnSubmissionCompleted;

        if (submitNameButton != null)
        {
            submitNameButton.onClick.RemoveListener(OnSubmitNameClicked);
        }
    }

    private async void OnSubmissionCompleted(SubmitScoreResponse response)
    {
        await WaitForSubmitResultAsync();
    }

    private async Task WaitForSubmitResultAsync()
    {
        ResolveReferences();

        Task<SubmitScoreResponse> submissionTask = leaderboardManager?.CurrentSubmissionTask;
        if (submissionTask != null)
        {
            SetStatusText("Submitting score...");
            await submissionTask;
        }

        RefreshSubmitResult();
    }

    private void RefreshSubmitResult()
    {
        ResolveReferences();
        SetLoadingVisible(false);

        if (leaderboardManager == null)
        {
            SetStatusText("Leaderboard unavailable.");
            SetNamePromptVisible(false);
            LogError("LeaderboardManager is missing.");
            return;
        }

        SubmitScoreResponse response = leaderboardManager.LastSubmitResponse;

        if (response == null)
        {
            SetStatusText(string.IsNullOrWhiteSpace(leaderboardManager.LastError)
                ? "Score was not submitted."
                : leaderboardManager.LastError);
            SetNamePromptVisible(false);
            return;
        }

        SetRankText(response.rank > 0 ? $"Rank #{response.rank}" : string.Empty);

        if (!response.accepted)
        {
            SetStatusText(string.IsNullOrWhiteSpace(response.error)
                ? "Score was not accepted."
                : response.error);
            SetNamePromptVisible(false);
            return;
        }

        if (response.isTop50)
        {
            SetStatusText("Top 50 score.");
            SetNamePromptVisible(true);
            return;
        }

        SetStatusText("Score submitted.");
        SetNamePromptVisible(false);
    }

    private async void OnSubmitNameClicked()
    {
        await SubmitNameAsync();
    }

    public async Task SubmitNameAsync()
    {
        if (isUpdatingName)
        {
            return;
        }

        ResolveReferences();

        SubmitScoreResponse submitResponse = leaderboardManager?.LastSubmitResponse;
        if (submitResponse == null || submitResponse.scoreEntryId <= 0)
        {
            SetStatusText("Score entry unavailable.");
            return;
        }

        string displayName = displayNameInput != null
            ? displayNameInput.text.Trim()
            : string.Empty;

        if (string.IsNullOrWhiteSpace(displayName))
        {
            SetStatusText("Enter a name first.");
            return;
        }

        isUpdatingName = true;
        SetSubmitButtonInteractable(false);
        SetStatusText("Saving name...");

        try
        {
            UpdateScoreNameResponse updateResponse = await ResolveLeaderboardManager().UpdateScoreNameAsync(
                submitResponse.scoreEntryId,
                displayName);

            if (updateResponse == null || !updateResponse.accepted)
            {
                string error = updateResponse == null || string.IsNullOrWhiteSpace(updateResponse.error)
                    ? "Could not save name."
                    : updateResponse.error;

                SetStatusText(error);
                return;
            }

            SetStatusText("Name saved.");
            SetNamePromptVisible(false);
            LogDebug($"Name saved for scoreEntryId={updateResponse.scoreEntryId}");
        }
        catch (Exception ex)
        {
            SetStatusText("Could not save name.");
            LogError($"Name update failed: {ex.Message}");
        }
        finally
        {
            isUpdatingName = false;
            SetSubmitButtonInteractable(true);
        }
    }

    private void ResolveReferences()
    {
        if (leaderboardManager == null)
        {
            leaderboardManager = LeaderboardManager.Instance;
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

    private void SetLoadingVisible(bool isVisible)
    {
        if (loadingRoot != null)
        {
            loadingRoot.SetActive(isVisible);
        }
    }

    private void SetNamePromptVisible(bool isVisible)
    {
        if (namePromptRoot != null)
        {
            namePromptRoot.SetActive(isVisible);
        }
    }

    private void SetSubmitButtonInteractable(bool isInteractable)
    {
        if (submitNameButton != null)
        {
            submitNameButton.interactable = isInteractable;
        }
    }

    private void SetStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void SetRankText(string message)
    {
        if (rankText != null)
        {
            rankText.text = message;
        }
    }

    #region Debug Logging

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[LeaderboardBreakdownController] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[LeaderboardBreakdownController] {message}");
    }

    #endregion
}
