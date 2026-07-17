using UnityEngine;
using UnityEngine.SceneManagement;

// Συντονίζει όλα τα συστήματα, τα μενού και την παύση
public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [Header("Configuration")]
    public string defaultScenarioName = "icu_scenario_hypoxia";
    public bool autoStartScenario = false;

    [Header("Managers")]
    public GameStateManager gameStateManager;
    public ScenarioManager scenarioManager;
    public DecisionTreeEngine decisionTreeEngine;
    public LoggingManager loggingManager;
    public DebriefingManager debriefingManager;
    public EHRManager ehrManager;
    public HotspotManager hotspotManager;
    public UIManager uiManager;
    public HelpSystem helpSystem;
    public ScenarioDebugOverlay scenarioDebugOverlay;
    public SceneHotspotBootstrap sceneHotspotBootstrap;
    public ICUAudioManager audioManager;
    public VitalsMonitorDisplay vitalsMonitorDisplay;

    [Header("State")]
    public bool isGameRunning = false;
    public bool isPaused = false;

    // UI της παύσης
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle subtitleStyle;
    private Texture2D bgTex;
    private Texture2D btnTex;
    private Texture2D btnHoverTex;
    private bool stylesBuilt = false;

    // Flags για να μην σκάει όταν κάνουμε restart/quit μέσα από το OnGUI
    private bool pendingRestart = false;
    private bool pendingMainMenu = false;

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
            return;
        }

        EnsureManagers();
    }

    private void Start()
    {
        if (decisionTreeEngine != null)
            decisionTreeEngine.OnScenarioCompleted += OnScenarioCompleted;

        scenarioManager?.LoadAvailableScenarios();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        // Εκτέλεση pending actions εκτός OnGUI
        if (pendingRestart)
        {
            pendingRestart = false;
            RestartScenario();
        }

        if (pendingMainMenu)
        {
            pendingMainMenu = false;
            ReturnToMainMenu();
        }

        // Παύση με ESC — μόνο αν δεν είναι ανοιχτό κάποιο πάνελ
        if (Input.GetKeyDown(KeyCode.Escape) && isGameRunning)
        {
            if (scenarioDebugOverlay != null && scenarioDebugOverlay.showOverlay)
            {
                scenarioDebugOverlay.showOverlay = false;
                scenarioDebugOverlay.showEhrPanel = false;
                LockCursor();
                return;
            }

            TogglePause();
        }

        // Shortcut (M) για το monitor
        if (Input.GetKeyDown(KeyCode.M) && isGameRunning && !isPaused)
        {
            VitalsMonitorDisplay.Instance?.Toggle();
        }
    }

    private void OnGUI()
    {
        if (!isPaused) return;

        if (!stylesBuilt) BuildPauseStyles();

        // Σκούρο background
        Color prev = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.65f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prev;

        float panelW = 340f;
        float panelH = 340f;
        float px = (Screen.width - panelW) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;

        GUI.Box(new Rect(px, py, panelW, panelH), "", panelStyle);

        GUI.Label(new Rect(px, py + 20f, panelW, 44f), "⏸  Παύση", titleStyle);
        GUI.Label(new Rect(px, py + 62f, panelW, 24f), "Το σενάριο είναι σε παύση", subtitleStyle);

        float btnW = 240f;
        float btnH = 48f;
        float btnX = px + (panelW - btnW) * 0.5f;
        float btnY = py + 108f;
        float gap = 10f;

        // Συνέχεια
        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "▶   Συνέχεια  [ESC]", buttonStyle))
        {
            ResumeGame();
        }

        btnY += btnH + gap;

        // Επανεκκίνηση 
        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "↺   Επανεκκίνηση Σεναρίου", buttonStyle))
        {
            ResumeGame();
            pendingRestart = true;
        }

        btnY += btnH + gap;

        // Κύριο Μενού
        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "⌂   Κύριο Μενού", buttonStyle))
        {
            ResumeGame();
            pendingMainMenu = true;
        }

        btnY += btnH + gap;

        // Έξοδος
        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "✕   Έξοδος από την Εφαρμογή", buttonStyle))
        {
            QuitApplication();
        }
    }

    // Κρύβει και κλειδώνει το ποντίκι για το 3D κομμάτι
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Ελευθερώνει το ποντίκι για να πατάμε κουμπιά στο UI
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1;
        AudioListener.pause = false;
        LockCursor();
    }

    private void BuildPauseStyles()
    {
        stylesBuilt = true;

        bgTex = new Texture2D(2, 2);
        Color bgCol = new Color(0.07f, 0.09f, 0.13f, 0.97f);
        bgTex.SetPixels(new[] { bgCol, bgCol, bgCol, bgCol });
        bgTex.Apply();

        btnTex = new Texture2D(2, 2);
        Color btnCol = new Color(0.16f, 0.20f, 0.28f, 1f);
        btnTex.SetPixels(new[] { btnCol, btnCol, btnCol, btnCol });
        btnTex.Apply();

        btnHoverTex = new Texture2D(2, 2);
        Color btnHoverCol = new Color(0.22f, 0.42f, 0.72f, 1f);
        btnHoverTex.SetPixels(new[] { btnHoverCol, btnHoverCol, btnHoverCol, btnHoverCol });
        btnHoverTex.Apply();

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = bgTex },
            border = new RectOffset(4, 4, 4, 4)
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.9f, 0.95f, 1f) }
        };

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.55f, 0.60f, 0.68f) }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal  = { background = btnTex,      textColor = new Color(0.85f, 0.90f, 1f) },
            hover   = { background = btnHoverTex, textColor = Color.white },
            border  = new RectOffset(4, 4, 4, 4),
            padding = new RectOffset(20, 10, 10, 10)
        };
    }

    #region Managers

    private void EnsureManagers()
    {
        if (gameStateManager == null)
            gameStateManager = FindOrCreateManager<GameStateManager>();
        if (scenarioManager == null)
            scenarioManager = FindOrCreateManager<ScenarioManager>();
        if (decisionTreeEngine == null)
            decisionTreeEngine = FindOrCreateManager<DecisionTreeEngine>();
        if (loggingManager == null)
            loggingManager = FindOrCreateManager<LoggingManager>();
        if (debriefingManager == null)
            debriefingManager = FindOrCreateManager<DebriefingManager>();
        if (ehrManager == null)
            ehrManager = FindOrCreateManager<EHRManager>();
        if (hotspotManager == null)
            hotspotManager = FindOrCreateManager<HotspotManager>();
        if (uiManager == null)
            uiManager = FindOrCreateManager<UIManager>();
        if (helpSystem == null)
            helpSystem = FindOrCreateManager<HelpSystem>();
        if (scenarioDebugOverlay == null)
            scenarioDebugOverlay = FindOrCreateManager<ScenarioDebugOverlay>();
        if (sceneHotspotBootstrap == null)
            sceneHotspotBootstrap = FindOrCreateManager<SceneHotspotBootstrap>();
        if (audioManager == null)
            audioManager = FindOrCreateManager<ICUAudioManager>();
        if (vitalsMonitorDisplay == null)
            vitalsMonitorDisplay = FindOrCreateManager<VitalsMonitorDisplay>();

        if (FindObjectOfType<AmbientParticles>() == null)
        {
            GameObject particleGo = new GameObject("AmbientParticles");
            particleGo.AddComponent<AmbientParticles>();
        }

        EnsureCameraInteraction();
    }

    private T FindOrCreateManager<T>() where T : MonoBehaviour
    {
        T manager = FindObjectOfType<T>();
        if (manager == null)
        {
            GameObject go = new GameObject(typeof(T).Name);
            manager = go.AddComponent<T>();
            Debug.Log($"Created {typeof(T).Name}");
        }
        return manager;
    }

    private void EnsureCameraInteraction()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        HotspotRaycaster raycaster = mainCamera.GetComponent<HotspotRaycaster>();
        if (raycaster == null)
        {
            raycaster = mainCamera.gameObject.AddComponent<HotspotRaycaster>();
            raycaster.interactDistance = 6f;
        }
    }

    #endregion

    #region Game Flow

    public void StartGame(string scenarioName)
    {
        ResetGame();

        bool loaded = scenarioManager.LoadScenario(scenarioName);
        if (!loaded)
        {
            Debug.LogError($"Failed to load scenario: {scenarioName}");
            uiManager?.ShowToast("❌ Αποτυχία φόρτωσης σεναρίου", "error");
            return;
        }

        uiManager?.ShowGameplay();
        sceneHotspotBootstrap?.EnsureHotspotsInScene();

        if (GameStateManager.Instance != null && hotspotManager != null)
            hotspotManager.SetActiveHotspots(GameStateManager.Instance.activeHotspots);

        decisionTreeEngine.StartScenario();

        isGameRunning = true;
        isPaused = false;

        LockCursor();

        if (scenarioDebugOverlay != null)
            scenarioDebugOverlay.showOverlay = false;

        Debug.Log($"Game started with scenario: {scenarioName}");
    }

    public void RestartScenario()
    {
        if (scenarioManager?.currentScenario != null)
        {
            string scenarioId = scenarioManager.currentScenario.scenario_meta.id;
            StartGame(scenarioId);
        }
    }

    public void TogglePause()
    {
        if (!isGameRunning) return;

        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0;
            AudioListener.pause = true;
            UnlockCursor();
        }
        else
        {
            ResumeGame();
        }
    }

    public void ReturnToMainMenu()
    {
        ResetGame();
        isGameRunning = false;
        isPaused = false;

        if (scenarioDebugOverlay != null)
        {
            scenarioDebugOverlay.showOverlay = true;
            scenarioDebugOverlay.showMainMenu = true;
        }

        UnlockCursor();
    }

    private void ResetGame()
    {
        gameStateManager?.ResetState();
        ehrManager?.ClearAllForms();
        loggingManager?.ClearLogs();

        VitalsMonitorDisplay.Instance?.Hide();
        ehrManager?.HideEHRPanel();

        if (scenarioDebugOverlay != null)
        {
            scenarioDebugOverlay.showOverlay = false;
            scenarioDebugOverlay.showEhrPanel = false;
        }
        scenarioDebugOverlay?.ClearToasts();
        AudioListener.pause = false;
        Time.timeScale = 1;
        isPaused = false;
    }

    private void OnScenarioCompleted()
    {
        isGameRunning = false;
        Debug.Log("Scenario completed!");
    }

    public void QuitApplication()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public string[] GetAvailableScenarios()
    {
        if (scenarioManager != null)
            return scenarioManager.availableScenarios.ToArray();
        return new string[0];
    }

    #endregion
}