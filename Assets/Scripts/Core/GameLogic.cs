using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLogic : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int totalRounds = 5;
    [SerializeField] private string mapPackName = "all";

    [Header("Managers")]
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private PreloadManager preloadManager;
    [SerializeField] private SceneFlowManager sceneFlowManager;
    [SerializeField] private MapPackManager mapPackManager;
    [SerializeField] private LocationManager locationManager;

    public static GameLogic Instance { get; private set; }

    private GameState currentState = GameState.Menu;
    private readonly GameSessionData session = new();

    private void Awake()
    {
        SetupSingleton();
    }

    private void Start()
    {
        RegisterEvents();
        InitializeGame();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }

        Destroy(gameObject);
    }

    private void RegisterEvents()
    {
        MapInteractionManager.OnGuessSubmitted += OnGuessSubmitted;
        MapInteractionManager.OnScoreCalculated += OnScoreCalculated;
    }

    private void InitializeGame()
    {
        if (!mapPackManager.ResolveMapPack(mapPackName))
        {
            Debug.LogError("Failed to resolve map pack.");
            return;
        }

        currentState = GameState.Loading;

        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        locationManager.SetCurrentMapPack(
            mapPackManager.ResolvedMapPackId);

        preloadManager.QueueNextLocation(
            session,
            locationManager.GetCurrentMapPack(),
            null);

        yield return StartCoroutine(
            roundManager.StartRound(session, totalRounds));

        currentState = GameState.Guessing;
    }

    private void OnGuessSubmitted(MapInteractionManager.LocationData guessLocation)
    {
        currentState = GameState.RoundEnd;

        AudioManager.Instance.PlayGuessSFX();

        var location = locationManager.GetCurrentLocation();

        MapInteractionManager.Instance?.SendActualLocationToJavaScript(
            location.latitude,
            location.longitude,
            location.zLevel);

        MapInteractionManager.Instance?.ShowBothLocations();

        if (session.CurrentRound < totalRounds)
        {
            preloadManager.QueueNextLocation(
                session,
                locationManager.GetCurrentMapPack(),
                location.ID);
        }
    }

    private void OnScoreCalculated(
        int score,
        int distance,
        int floorDiff,
        bool tooHigh)
    {
        scoreManager.ApplyScore(
            session,
            score,
            distance,
            floorDiff,
            tooHigh);

        StartCoroutine(NextRoundRoutine());
    }

    private IEnumerator NextRoundRoutine()
    {
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        if (session.CurrentRound >= totalRounds)
        {
            EndGame();
            yield break;
        }

        session.CurrentRound++;

        yield return StartCoroutine(
            roundManager.StartRound(session, totalRounds));

        currentState = GameState.Guessing;
    }

    private void EndGame()
    {
        currentState = GameState.GameEnd;

        MapInteractionManager.Instance?.HideMap();

        sceneFlowManager.LoadBreakdownScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SceneHelper.IsMenuScene(scene.name))
        {
            MapInteractionManager.Instance?.HideMap();
        }
    }
}
