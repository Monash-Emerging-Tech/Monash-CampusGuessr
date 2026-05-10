using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IntroSkipController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayableDirector introDirector;
    [SerializeField] private Animator mainCameraAnimator;
    [SerializeField] private MenuScreenAnimationControl menuScreenAnimationControl;
    [SerializeField] private GameObject skipHintRoot;
    [SerializeField] private Image holdProgressRing;

    [Header("Hold To Skip")]
    [SerializeField] private float holdDuration = 1f;
    [SerializeField] private float releaseDecaySpeed = 2.5f;
    [SerializeField] private float hintOutMoveDown = 24f;
    [SerializeField] private float hintOutLeadTime = 0.5f;

    [Header("Camera Animator")]
    [SerializeField] private string cameraIntroStateName = "MainMenuCameraStart";

    private float holdTimer;
    private bool hasSkipped;
    private bool introFinished;
    private bool isAnimatingHintOut;
    private RectTransform skipHintRect;
    private CanvasGroup skipHintCanvasGroup;
    private Vector2 skipHintBaseAnchoredPos;

    private void OnEnable()
    {
        introDirector.stopped += HandleDirectorStopped;
    }

    private void OnDisable()
    {
        introDirector.stopped -= HandleDirectorStopped;
    }

    private void Start()
    {
        skipHintRect = skipHintRoot.GetComponent<RectTransform>();
        if (skipHintRect) {
            skipHintBaseAnchoredPos = skipHintRect.anchoredPosition;
        }

        skipHintCanvasGroup = skipHintRoot.GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (isAnimatingHintOut)
        {
            UpdateHintOutAnimation();
            return;
        }

        if (hasSkipped || introFinished)
        {
            return;
        }

        StartHintOut();

        if (Input.GetMouseButton(0))
        {
            holdTimer += Time.unscaledDeltaTime;
        }
        else
        {
            holdTimer = Mathf.MoveTowards(holdTimer, 0f, releaseDecaySpeed * Time.unscaledDeltaTime);
        }

        float progress = Mathf.Clamp01(holdTimer / holdDuration);
        UpdateRingFill(progress);

        if (progress >= 1f)
        {
            SkipIntro();
        }
    }

    private void HandleDirectorStopped(PlayableDirector director)
    {
        if (director == introDirector && !hasSkipped)
        {
            MarkIntroFinished(false);
        }
    }

    [ContextMenu("Skip Intro Now")]
    public void SkipIntro()
    {
        if (hasSkipped || introFinished)
        {
            return;
        }

        hasSkipped = true;

        introDirector.time = introDirector.duration;
        introDirector.Evaluate();
        introDirector.Pause();

        if (!string.IsNullOrEmpty(cameraIntroStateName))
        {
            mainCameraAnimator.Play(cameraIntroStateName, 0, 1f);
            mainCameraAnimator.Update(0f);
        }

        menuScreenAnimationControl.ShowUI();

        menuScreenAnimationControl.SetParallaxEnabled(false);
        MarkIntroFinished(true);
    }

    private void MarkIntroFinished(bool endedBySkip)
    {
        introFinished = true;

        if (!endedBySkip)
        {
            isAnimatingHintOut = true;
            return;
        }

        SetSkipHintVisible(false);
    }

    private void UpdateRingFill(float progress)
    {
        holdProgressRing.fillAmount = progress;
    }

    private void SetSkipHintVisible(bool visible)
    {
        skipHintRoot.SetActive(visible);
    }

    private void UpdateHintOutAnimation()
    {
        float lead = Mathf.Max(0.01f, hintOutLeadTime);
        float remaining = (float)(introDirector.duration - introDirector.time);
        float t = Mathf.Clamp01((lead - remaining) / lead);

        skipHintCanvasGroup.alpha = 1f - t;

        float yOffset = Mathf.Lerp(0f, -hintOutMoveDown, t);
        skipHintRect.anchoredPosition = skipHintBaseAnchoredPos + new Vector2(0f, yOffset);

        if (t >= 1f)
        {
            isAnimatingHintOut = false;
            SetSkipHintVisible(false);
        }
    }

    private void StartHintOut()
    {
        if (isAnimatingHintOut || introDirector.state != PlayState.Playing)
        {
            return;
        }

        double remaining = introDirector.duration - introDirector.time;
        float triggerWindow = Mathf.Max(0.01f, hintOutLeadTime);

        if (remaining <= triggerWindow)
        {
            isAnimatingHintOut = true;
        }
    }
}
