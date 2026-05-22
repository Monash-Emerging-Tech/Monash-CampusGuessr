using UnityEngine;

/// CameraZoomButtons
/// Written by Madison 



public class CameraZoomButtons : MonoBehaviour
{
    [Header("Assign")]
    [SerializeField] private Camera cam;        // drag Main Camera here (or it will auto-find)
    [SerializeField] private Transform pivot;   // drag ZoomPivot here

    [Header("Perspective (dolly)")]
    [SerializeField] private float minDistance = 5f;   // how close camera can get
    [SerializeField] private float maxDistance = 50f;  // how far it can go
    [SerializeField] private float distanceStep = 2f;  // per click

    [Header("Orthographic (if your camera is ortho)")]
    [SerializeField] private float minOrtho = 5f;
    [SerializeField] private float maxOrtho = 50f;
    [SerializeField] private float orthoStep = 2f;

    [Header("Alternative for perspective (optional)")]
    [SerializeField] private bool useFOVInsteadOfDolly = false;
    [SerializeField] private float minFOV = 25f, maxFOV = 60f, fovStep = 5f;

    private float currentDistance;

    private void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("CameraZoomButtons: No Camera assigned."); enabled = false; return; }

        // Clamp sensible mins
        minDistance = Mathf.Max(minDistance, cam.nearClipPlane + 0.1f);

        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minOrtho, maxOrtho);
        }
        else if (!useFOVInsteadOfDolly)
        {
            if (!pivot)
            {
                // Auto-create a pivot 10 units in front if you forgot to assign one
                var go = new GameObject("AutoZoomPivot");
                go.transform.position = cam.transform.position + cam.transform.forward * 10f;
                pivot = go.transform;
            }

            currentDistance = Vector3.Distance(cam.transform.position, pivot.position);
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            cam.transform.position = pivot.position - cam.transform.forward * currentDistance;
        }
    }

    public void ZoomIn()  => Zoom(+1);   // closer
    public void ZoomOut() => Zoom(-1);   // farther

    private void Zoom(int direction)
    {
        // direction: +1 = in, -1 = out
        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize - direction * orthoStep,
                minOrtho, maxOrtho);
        }
        else if (useFOVInsteadOfDolly)
        {
            cam.fieldOfView = Mathf.Clamp(
                cam.fieldOfView - direction * fovStep,
                minFOV, maxFOV);
        }
        else
        {
            currentDistance = Mathf.Clamp(
                currentDistance - direction * distanceStep,
                minDistance, maxDistance);

            cam.transform.position = pivot.position - cam.transform.forward * currentDistance;
        }
    }
}

