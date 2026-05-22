using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/***
 * 
 * ToggleSwitch, Responsible for toggling ON/OFF different game settings 
 * Will need to develop attach specific game setting to button
 *
 * Written by aleu0007
 * Last Modified: 13/08/2025
 * 
 */
public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
{
    [Header("Button Setup")]
    public bool CurrentValue { get; private set; }
    
    [Header("Setting Configuration")]
    [SerializeField] private string settingName = "DefaultSetting"; // The name of the setting this toggle controls eg. timer / practice mode
    [SerializeField] private Sprite onStateSprite; // Sprite to display when toggle is ON
    [SerializeField] private Sprite offStateSprite; // Sprite to display when toggle is OFF
    
    private Button _button;
    private Image _buttonImage;

    [Header("Events")]
    [SerializeField] private UnityEvent onToggleOn;
    [SerializeField] private UnityEvent onToggleOff;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupToggleComponents();
        UpdateButtonAppearance();
    }

    protected void OnValidate()
    {
        SetupToggleComponents();
        UpdateButtonAppearance();
    }

    private void SetupToggleComponents()
    {
        if (_button != null)
            return;

        SetupButtonComponent();
    }

    private void SetupButtonComponent()
    {
        _button = GetComponent<Button>();

        if (_button == null)
        {
            Debug.LogError("No Button component found on " + gameObject.name + "! Please attach this script to a GameObject with a Button component.", this);
            return;
        }

        // Get the Image component from the button itself
        _buttonImage = _button.GetComponent<Image>();
        
        if (_buttonImage == null)
        {
            Debug.LogError("No Image component found on " + gameObject.name + ". Button image will not be updated.", this);
        }
    }

    protected virtual void Awake()
    {
        SetupToggleComponents();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Toggle();
    }

    private void Toggle()
    {
        SetState(!CurrentValue);
    }

    private void SetState(bool state)
    {
        CurrentValue = state;
        UpdateButtonAppearance();
        
        // Log the current state to console
        Debug.Log($"[{settingName}] Toggle Button State: {(CurrentValue ? "ON" : "OFF")}");

        if (CurrentValue)
            onToggleOn?.Invoke();
        else
            onToggleOff?.Invoke();
    }
    
    private void UpdateButtonAppearance()
    {
        if (_buttonImage != null)
        {
            // Update the button's sprite based on current state
            _buttonImage.sprite = CurrentValue ? onStateSprite : offStateSprite;
        }
        else
        {
            Debug.LogWarning("Button Image component not found. Cannot update sprite.", this);
        }
        
        // Validate that sprites are assigned
        if (onStateSprite == null || offStateSprite == null)
        {
            Debug.LogWarning($"[{settingName}] ON or OFF sprite is not assigned. Please assign sprites in the inspector.", this);
        }
    }
    
    /// <summary>
    /// Public method to set the toggle state externally
    /// </summary>
    /// <param name="state">The desired state (true = ON, false = OFF)</param>
    public void SetToggleState(bool state)
    {
        SetState(state);
    }
    
    /// <summary>
    /// Public method to get the current setting name
    /// </summary>
    /// <returns>The name of the setting this toggle controls</returns>
    public string GetSettingName()
    {
        return settingName;
    }
}
