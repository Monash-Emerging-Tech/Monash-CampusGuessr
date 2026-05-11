using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneCategory
{
    Menu,
    Campus,
    Testing,
    Shared
}

public class SceneController : MonoBehaviour
{
    public static SceneController Instance;

    public SceneCategory currentCategory;

    public CampusType currentCampus;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =========================
    // MENUS
    // =========================

    public void LoadMainMenu()
    {
        currentCategory = SceneCategory.Menu;
        currentCampus = CampusType.None;

        SceneManager.LoadScene("MainMenu");
    }

    public void LoadMapSelection()
    {
        currentCategory = SceneCategory.Menu;
        currentCampus = CampusType.None;

        SceneManager.LoadScene("MapSelection");
    }

    // =========================
    // CAMPUSES
    // =========================

    public void LoadCampus(CampusType campus)
    {
        currentCategory = SceneCategory.Campus;

        currentCampus = campus;

        string sceneName =
            CampusDatabase.Campuses[campus];

        SceneManager.LoadScene(sceneName);
    }

    // =========================
    // HELPERS
    // =========================

    public bool IsMenu()
    {
        return currentCategory == SceneCategory.Menu;
    }

    public bool IsGameplay()
    {
        return currentCategory == SceneCategory.Campus;
    }
}