using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/***
 * 
 * TooltipTrigger, Responsible for showing and hiding tooltips on UI elements
 *
 * Written by aleu0007
 * Last Modified: 11/02/2026
 * 
 */
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler

{
    [SerializeField] private GameObject tooltipBox;
    private CanvasGroup tooltipCanvasGroup;


    public void OnPointerEnter(PointerEventData eventData)
    {
        SetTooltipVisible(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetTooltipVisible(false);
    }

    private void Awake()
    {
        if (tooltipBox != null)
        {
            if (!tooltipBox.activeSelf)
                tooltipBox.SetActive(true);

            tooltipCanvasGroup = tooltipBox.GetComponent<CanvasGroup>();
            if (tooltipCanvasGroup == null)
                tooltipCanvasGroup = tooltipBox.AddComponent<CanvasGroup>();

            SetTooltipVisible(false);
        }
    }

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalCall("setHasTooltipBox", true);
#endif
    }

    private void OnDestroy()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalCall("setHasTooltipBox", false);
#endif
    }

    public void ShowTooltipFromWeb(string secondsStr)
    {
        float seconds = 5f;
        float.TryParse(secondsStr, out seconds);
        if (tooltipBox == null)
            return;

        SetTooltipVisible(true);
        CancelInvoke(nameof(HideTooltip));
        Invoke(nameof(HideTooltip), seconds);
    }

    private void HideTooltip()
    {
        SetTooltipVisible(false);
    }

    private void SetTooltipVisible(bool isVisible)
    {
        if (tooltipBox == null)
            return;

        if (tooltipCanvasGroup == null)
            tooltipCanvasGroup = tooltipBox.GetComponent<CanvasGroup>();

        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = isVisible ? 1f : 0f;
            tooltipCanvasGroup.blocksRaycasts = isVisible;
            tooltipCanvasGroup.interactable = isVisible;
        }
        else
        {
            tooltipBox.SetActive(isVisible);
        }
    }
}