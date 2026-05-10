using Unity.VisualScripting;
using UnityEngine;

public class MenuScreenAnimationControl : MonoBehaviour
{

    // Showing the Menu UI
    public GameObject canvasToShow;
    private bool menuUI = false;


    public float moveStrength = 1f;    // How far camera moves in 3D space
    public float smoothTime = 0.2f;    // How smooth the movement is
    [SerializeField] private bool enableMouseParallax = true;

    private Vector3 startPos;
    private Vector3 velocity;


    void Start()
    {
        // Hide at the start
        canvasToShow.SetActive(false);
    }

    void Update()
    {
        
        if (menuUI && enableMouseParallax) {

            if (!menuUI) return;

            Vector2 mouse = Camera.main.ScreenToViewportPoint(Input.mousePosition)
                            - new Vector3(0.5f, 0.5f);

            Vector3 targetPos = startPos + new Vector3(mouse.x, mouse.y, 0f) * moveStrength;

            transform.position = Vector3.SmoothDamp(transform.position, targetPos,
                                                    ref velocity, smoothTime);
        }

    }

    // Called by Animation Event
    public void ShowUI()
    {

        startPos = transform.position;

        canvasToShow.SetActive(true);
        menuUI = true;
    }

    public void SetParallaxEnabled(bool enabled)
    {
        enableMouseParallax = enabled;

        if (!enabled && menuUI)
        {
            transform.position = startPos;
            velocity = Vector3.zero;
        }
    }
}
