using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;


/***
* RotateCamera.cs, used to handle rotating the camera by clicking and dragging the left mouse button 
* Written by: O_Bolt
* Last Modified: 15/07/25
*/



public class RotateCamera : MonoBehaviour
{

    // OriginPoint, set to an object at 0,0,0 not sure if it actual matters where, we just need a reference to it
    public Transform originPoint;          

    // Sensitivity of the mouse, likely to be controlled in the actual in-game settings
    public float mouseSensitivity = 5.0f;


    // Make private after testing
    public float smoothingFactor = 0.1f; 
    public float maxMouseDelta = 1.0f;    // Max allowed mouse movement per frame


    private float xSpeed = 120.0f;     // Speed of horizontal rotation
    private float ySpeed = 120.0f;     // Speed of vertical rotation


    // Limits on the y angle to make sure it doesn't flip
    private float xMinLimit = -80f;    
    private float xMaxLimit = 80f;     

    // Important to remember that this is a rotation around the axis, meaning looking up and down is around the x-axis
    private float x = 0.0f;
    private float y = 0.0f;

    private float smoothx = 0.0f;
    private float smoothy = 0.0f;


    // Not relevant in our case since we're not looking at an object, we're just moving the camera around the world
    // This changes to be Camera FOV for zooming, distance not important at all
    private float distance = 10f;

    // Left mouse button
    private int LEFT = 0;

    public List<GameObject> cameraDisabledUIElements = new List<GameObject>();
    private bool dragStartedOnUI = false;

    void Start()
    {

        // Quick Debug, always have an invisible object set at 0,0,0 so that the camera can rotate/orbit around
        if (originPoint == null)
        {
            Debug.LogError("No OriginPoint found for the Camera Rotation");
            enabled = false;
            return;
        }

        // Get camera's current angles
        Vector3 angles = transform.eulerAngles;
        x = angles.x;
        y = angles.y;
    }

    void LateUpdate()
    {
        if (GameLogic.Instance != null &&
            GameLogic.Instance.IsRoundActive() &&
            !GameLogic.Instance.IsGuessing())
        {
            return;
        }

        // declare this else where with a function once the user has ability to edit mouse sensitivity
        // 200 is a number I found works, let's get rid of this scaling factor at a later date
        xSpeed = mouseSensitivity*200;
        ySpeed = mouseSensitivity*200;



        // Check if the Mouse was clicked, this only happens on the first frame of the mouse being clicked
        if (Input.GetMouseButtonDown(LEFT))
        {
            dragStartedOnUI = IsMouseOverUI();  // Check if over a UI Element
        }

        // Left Mouse Button, check if it's down
        if (Input.GetMouseButton(LEFT)) 
        {
            // Don't let the camera move if the mouse is on UI
            if (dragStartedOnUI)
                return;

            // Get mouse delta input and clamp it to avoid large jumps
            float deltaX = Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
            float deltaY = Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;

            // Ensure the mouse movement speed did not change too rapidly during the frame
            deltaX = Mathf.Clamp(deltaX, -maxMouseDelta, maxMouseDelta);
            deltaY = Mathf.Clamp(deltaY, -maxMouseDelta, maxMouseDelta);



            // Apply smoothing to mouse movement by averaging the current and previous deltas
            // Lerp function ensures that the new rotation speed is no more than a 10% increase of the previous rotation speed (given default values)
            // ^^ above statement may be false actually, I've got no clue what I'm talking about
            smoothx = Mathf.Lerp(smoothx, deltaX, smoothingFactor);
            smoothy = Mathf.Lerp(smoothy, deltaY, smoothingFactor);


            // Change these to invert controls, controls currently align with google maps
            x -= smoothx;
            y += smoothy;

            // Ensure the angle is between it's flipping limits
            y = Mathf.Clamp(y, xMinLimit, xMaxLimit);
        }

        // Position the camera now given the new rotation with reference to the originpoint
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + originPoint.position;

        transform.rotation = rotation;
        transform.position = position;
    }


    // Checks if the mouse is currently over one of the excluded UI that is set in the unity project
    private bool IsMouseOverUI()
    {

        // Make sure Eventsystem is active
        if (EventSystem.current == null)
            return false;

        // Create the mouse pointer event, set to current mouse position
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        // RayCastAll see's if there is a UI element under the mouse 
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            // If a UI Element that the mouse is not allowed to be over is in the results, then disable the camera movement
            if (cameraDisabledUIElements.Contains(result.gameObject))
                return true;
        }

        return false;
    }
}