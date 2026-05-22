using UnityEngine;
using TMPro;
using System.Linq;
using static Unity.Burst.Intrinsics.X86.Avx;



/// Debug Log Script
/// 
/// Displays text information on screen about to current state of the Game
/// 
/// Written by O-Bolt
/// 



public class DebugLogScript : MonoBehaviour
{
    private GameObject gameManager;
    private GameLogic gameLoader;
    private TMP_Text text;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("DebugLogScript starting on: " + gameObject.name);

        // Make sure a gamemanager is connected
        gameManager = GameObject.Find("GameLogic");
        if (gameManager != null)
        {
            gameLoader = gameManager.GetComponent<GameLogic>();
            if (gameLoader == null)
            {
                Debug.LogError("GameLogic component not found on GameManager!");
            }
        }
        else
        {
            Debug.LogWarning("GameManager not found!");
        }

        // Get the TMP_Text component (check both this object and children)
        text = GetComponent<TMP_Text>();
        if (text == null)
        {
            Debug.Log("TMP_Text not found on this object, checking children...");
            text = GetComponentInChildren<TMP_Text>();
        }
        if (text == null)
        {
            Debug.LogError("TMP_Text component not found on " + gameObject.name + " or its children!");
            Debug.LogError("Available components on this object: " + string.Join(", ", GetComponents<Component>().Select(c => c.GetType().Name)));
        }
        else
        {
            Debug.Log("Debug Log TMP_Text component found on: " + text.gameObject.name);
        }

    }

    // Update is called once per frame
    void Update()
    {
        // Display on the text field provided
        if (gameManager != null && gameLoader != null && text != null)
        {
            try
            {
                string debugText = "Debug Log:";

                // Safe method calls with try-catch for each
                try { debugText += "\r\ninGame: " + gameLoader.IsInGame(); } catch (System.Exception e) { debugText += "\r\ninGame: ERROR - " + e.Message; }
                try { debugText += "\r\nGuessing: " + gameLoader.IsGuessing(); } catch (System.Exception e) { debugText += "\r\nGuessing: ERROR - " + e.Message; }
                try { debugText += "\r\nCurrent Score: " + gameLoader.GetCurrentScore(); } catch (System.Exception e) { debugText += "\r\nCurrent Score: ERROR - " + e.Message; }
                try { debugText += "\r\nRound: " + gameLoader.GetCurrentRound(); } catch (System.Exception e) { debugText += "\r\nRound: ERROR - " + e.Message; }
                try { debugText += "\r\nMapPackId: " + gameLoader.GetMapPackId(); } catch (System.Exception e) { debugText += "\r\nMapPackId: ERROR - " + e.Message; }
                try { debugText += "\r\nMapPackName: " + gameLoader.GetMapPackName(); } catch (System.Exception e) { debugText += "\r\nMapPackName: ERROR - " + e.Message; }
                try
                {
                    var allMapPacks = gameLoader.GetAllMapPackNames();
                    debugText += "\r\nAvailable MapPacks: " + (allMapPacks.Length > 0 ? string.Join(", ", allMapPacks) : "None");
                }
                catch (System.Exception e)
                {
                    debugText += "\r\nAvailable MapPacks: ERROR - " + e.Message;
                }

                // Location manager calls - these are more likely to cause issues
                try
                {
                    var locationManager = gameLoader.GetLocationManager();
                    if (locationManager != null)
                    {
                        var currentLocation = locationManager.GetCurrentLocation();
                        // Check if location is valid (since it's a struct, check for default values)
                        if (!string.IsNullOrEmpty(currentLocation.Name))
                        {
                            debugText += "\r\nLocation: " + currentLocation.Name;
                            debugText += "\r\nLocation Lat: " + currentLocation.latitude;
                            debugText += "\r\nLocation Lng: " + currentLocation.longitude;
                            debugText += "\r\nLocation Z-Level: " + currentLocation.zLevel;
                        }
                        else
                        {
                            debugText += "\r\nLocation: No location set";
                        }
                    }
                    else
                    {
                        debugText += "\r\nLocation Manager: NULL";
                    }
                }
                catch (System.Exception e)
                {
                    debugText += "\r\nLocation: ERROR - " + e.Message;
                }

                text.text = debugText;
            }
            catch (System.Exception e)
            {
                text.text = "Debug Log: ERROR - " + e.Message;
                Debug.LogError("DebugLogScript Update Error: " + e.Message);
            }
        }
        else
        {
            if (text != null)
            {
                text.text = "Debug Log: Initializing...";
            }
        }

    }
}
