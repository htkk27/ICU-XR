using System.Collections.Generic;
using UnityEngine;

public class ScenarioDebugOverlay : MonoBehaviour
{
    public static ScenarioDebugOverlay Instance { get; private set; }

    // Ρυθμίσεις εμφάνισης
    [Header("Display")]
    public bool showOverlay = true;
    public bool showVitalsPanel = true;
    public bool showEhrPanel = true;

    private Vector2 optionScroll;
    private Vector2 ehrScroll;
    private Vector2 helpScroll;
    private Vector2 debriefScroll;

    private GUIStyle titleStyle;
    private GUIStyle boxStyle;
    private GUIStyle wrapLabelStyle;
    private GUIStyle toastStyle;
    private GUIStyle toastDangerStyle;
    private GUIStyle toastSuccessStyle;
    private GUIStyle toastWarningStyle;
    private GUIStyle scenarioBtnStyle;
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;

    // Σύστημα για τα μικρά μηνύματα (toasts) που πετάγονται
    private struct ToastMessage
    {
        public string text;
        public string style;
        public float expireTime;
    }
    private List<ToastMessage> activeToasts = new List<ToastMessage>();
    private float toastDuration = 4f;

    // Μεταβλητές για το Help
    private bool showHelp = false;
    private string currentHelpTopic = "general";

    // Κεντρικό μενού
    public bool showMainMenu = true;

    // Επιλογή σεναρίου
    private bool showScenarioSelect = false;

    // Οθόνη απολογισμού (Debrief)
    private DebriefData debriefData = null;
    private bool showDebrief = false;

    // Έξτρα στυλ γραφικών για το αρχικό μενού
    private GUIStyle menuTitleStyle;
    private GUIStyle menuSubtitleStyle;
    private GUIStyle menuButtonStyle;
    private GUIStyle menuDescStyle;
    private Texture2D menuBgTex;

    // Animations για το μενού
    private float menuTime;
    private float menuFadeIn;

    // Κρατάει ποιο σενάριο κοιτάμε με το ποντίκι (Hover)
    private string hoveredScenario = "";

    // Ο μινι χάρτης
    private bool showMinimap = true;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        if (DebriefingManager.Instance != null)
            DebriefingManager.Instance.OnDebriefingReady += OnDebriefReady;
    }

    private void OnDisable()
    {
        if (DebriefingManager.Instance != null)
            DebriefingManager.Instance.OnDebriefingReady -= OnDebriefReady;
    }

    private void Start()
    {
        if (DebriefingManager.Instance != null)
        {
            DebriefingManager.Instance.OnDebriefingReady -= OnDebriefReady;
            DebriefingManager.Instance.OnDebriefingReady += OnDebriefReady;
        }
    }

    private void OnDebriefReady(DebriefData data)
    {
        debriefData = data;
        showDebrief = true;
        showOverlay = true;
        GameController.Instance?.UnlockCursor();
    }

    public void AddToast(string text, string style)
    {
        activeToasts.Add(new ToastMessage
        {
            text = text,
            style = style,
            expireTime = Time.time + toastDuration
        });
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Για να μην ανοίγει το overlay αν είμαστε σε παύση
            if (GameController.Instance != null && GameController.Instance.isPaused) return;

            showOverlay = !showOverlay;
            showDebrief = false;
            showHelp = false;
            showScenarioSelect = false;

            if (showOverlay)
            {
                GameController.Instance?.UnlockCursor();
            }
            else
            {
                if (GameController.Instance != null && GameController.Instance.isGameRunning)
                    GameController.Instance?.LockCursor();
            }
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (GameController.Instance != null && GameController.Instance.isPaused) return;

            showHelp = !showHelp;
            if (showHelp)
            {
                showOverlay = true;
                showDebrief = false;
                showScenarioSelect = false;
                GameController.Instance?.UnlockCursor();
                
                // Παύση gameplay
                Time.timeScale = 0;
                AudioListener.pause = true;
                }
            else
            {
                Time.timeScale = 1;
                AudioListener.pause = false;
                if (GameController.Instance?.isGameRunning == true)
                    GameController.Instance?.LockCursor();
            }
    }

        activeToasts.RemoveAll(t => Time.time > t.expireTime);
    }

    private void OnGUI()
    {
        if (titleStyle == null) BuildStyles();

        if (showMainMenu)
        {
            DrawMainMenu();
            return;
        }

        DrawToasts();

        if (GameController.Instance != null && GameController.Instance.isGameRunning && !GameController.Instance.isPaused)
            {
                DrawStatusBar();
                if (showMinimap)
                    DrawMinimap();
        }
        
        if (!showOverlay) return;

        if (showDebrief && debriefData != null)
        {
            DrawDebriefScreen();
            return;
        }

        if (showHelp)
        {
            DrawHelpScreen();
            return;
        }

        if (showScenarioSelect)
        {
            DrawScenarioSelection();
            return;
        }

        if (ScenarioManager.Instance == null || DecisionTreeEngine.Instance == null ||
            DecisionTreeEngine.Instance.currentNode == null)
        {
            DrawNoScenarioScreen();
            return;
        }

        DrawScenarioPanel();

        if (showVitalsPanel)
            DrawVitalsPanel();

        if (showEhrPanel)
            DrawEhrPanel();

        DrawStatusBar();
    }

    // Μηνύματα (Toasts)

    private void DrawToasts()
    {
        if (activeToasts.Count == 0) return;

        float w = 500f;
        float x = (Screen.width - w) * 0.5f;
        float y = 60f;

        for (int i = 0; i < activeToasts.Count; i++)
        {
            var toast = activeToasts[i];
            float age = (toast.expireTime - Time.time);
            float lifeRatio = age / toastDuration;

            // Εφέ εμφάνισης/εξαφάνισης του μηνύματος
            float enterT = Mathf.Clamp01((toastDuration - age) / 0.3f);
            float exitT = Mathf.Clamp01(age / 0.5f);
            float slideY = Mathf.Lerp(-40f, 0f, enterT);
            float alpha = exitT;

            GUIStyle style = GetToastStyle(toast.style);
            float h = style.CalcHeight(new GUIContent(toast.text), w - 40f) + 18f;

            float drawY = y + slideY;
            Color prev = GUI.color;

            // Φόντο μηνύματος
            Color bgCol = toast.style == "danger" || toast.style == "error"
                ? new Color(0.2f, 0.05f, 0.05f, 0.92f * alpha)
                : toast.style == "success"
                    ? new Color(0.05f, 0.15f, 0.05f, 0.92f * alpha)
                    : toast.style == "warning"
                        ? new Color(0.2f, 0.15f, 0.02f, 0.92f * alpha)
                        : new Color(0.08f, 0.1f, 0.16f, 0.92f * alpha);

            GUI.color = bgCol;
            GUI.DrawTexture(new Rect(x, drawY, w, h), Texture2D.whiteTexture);

            // Χρωματιστή γραμμή στα αριστερά
            Color accentCol = toast.style == "danger" || toast.style == "error"
                ? new Color(1f, 0.3f, 0.3f, 0.8f * alpha)
                : toast.style == "success"
                    ? new Color(0.3f, 1f, 0.4f, 0.8f * alpha)
                    : toast.style == "warning"
                        ? new Color(1f, 0.85f, 0.2f, 0.8f * alpha)
                        : new Color(0.3f, 0.7f, 1f, 0.8f * alpha);
            GUI.color = accentCol;
            GUI.DrawTexture(new Rect(x, drawY, 4, h), Texture2D.whiteTexture);

            GUI.color = new Color(1, 1, 1, alpha);
            GUI.Label(new Rect(x + 16, drawY + 6, w - 32, h - 12), toast.text, style);

            GUI.color = prev;
            y += h + 6f;
        }
    }

    private GUIStyle GetToastStyle(string style)
    {
        switch (style)
        {
            case "danger": case "error": return toastDangerStyle;
            case "success": return toastSuccessStyle;
            case "warning": return toastWarningStyle;
            default: return toastStyle;
        }
    }

    public void ClearToasts()
    {
        activeToasts.Clear();
    }

    // Αρχικό Μενού 

    private void DrawMainMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        menuTime += Time.deltaTime;
        menuFadeIn = Mathf.Clamp01(menuFadeIn + Time.deltaTime * 1.5f);

        if (menuTitleStyle == null) BuildMenuStyles();

        float fade = menuFadeIn;

        // Σκοτεινό φόντο για το μενού
        GUI.color = new Color(0.02f, 0.04f, 0.06f, 0.92f * fade);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float centerX = Screen.width * 0.5f;

        // Το εφέ καρδιογραφήματος στο background
        DrawMenuECG(fade);

        // Διακοσμητικές γραμμές πάνω και κάτω
        GUI.color = new Color(0.15f, 0.6f, 0.85f, 0.4f * fade);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, Screen.height - 2, Screen.width, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Γωνίες για εφέ sci-fi/ιατρικής οθόνης
        DrawCornerMarkers(fade);

        float startY = Screen.height * 0.08f;

        // Σταυρός (ιατρικό εικονίδιο)
        GUI.color = new Color(0.2f, 0.75f, 0.9f, 0.35f * fade);
        float crossSize = 30f;
        GUI.DrawTexture(new Rect(centerX - crossSize * 0.5f, startY - 5, crossSize, 4), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - 2, startY - 5 - crossSize * 0.5f + 2, 4, crossSize), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Τίτλος με εφέ εμφάνισης
        float titleOffset = (1f - fade) * 30f;
        GUI.color = new Color(1, 1, 1, fade);
        Rect titleRect = new Rect(0, startY + 35 + titleOffset, Screen.width, 50);
        GUI.Label(titleRect, "ΠΡΟΣΟΜΟΙΩΣΗ ΜΕΘ", menuTitleStyle);

        // Υπότιτλος
        Rect subRect = new Rect(0, startY + 88 + titleOffset, Screen.width, 30);
        GUI.Label(subRect, "ICU Clinical Decision Simulation", menuSubtitleStyle);

        // Διαχωριστική γραμμή που πάλλεται
        float sepPulse = 0.3f + Mathf.Sin(menuTime * 2f) * 0.15f;
        GUI.color = new Color(0.2f, 0.7f, 0.9f, sepPulse * fade);
        float sepW = 200f + Mathf.Sin(menuTime * 0.8f) * 20f;
        GUI.DrawTexture(new Rect(centerX - sepW * 0.5f, startY + 128, sepW, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Περιγραφή
        float descY = startY + 148;
        GUI.color = new Color(1, 1, 1, fade * 0.9f);
        Rect descRect = new Rect(centerX - 260, descY, 520, 65);
        GUI.Label(descRect,
            "Εκπαιδευτικό εργαλείο κλινικής λήψης αποφάσεων σε Μονάδα Εντατικής Θεραπείας.\n" +
            "Αξιολογήστε ασθενείς, λάβετε αποφάσεις, τεκμηριώστε στο EHR.",
            menuDescStyle);

        // Κάρτες Σεναρίων
        float cardW = 420f;
        float cardH = 80f;
        float cardX = centerX - cardW * 0.5f;
        float cardY = descY + 80f;

        ScenarioManager.Instance?.LoadAvailableScenarios();
        var scenarios = GameController.Instance?.GetAvailableScenarios();

        GUI.color = new Color(0.45f, 0.65f, 0.8f, 0.7f * fade);
        GUI.Label(new Rect(0, cardY - 28, Screen.width, 22), "ΕΠΙΛΕΞΤΕ ΣΕΝΑΡΙΟ", menuSubtitleStyle);
        GUI.color = new Color(1, 1, 1, fade);

        if (scenarios != null && scenarios.Length > 0)
        {
            for (int i = 0; i < scenarios.Length; i++)
            {
                string name = scenarios[i];
                string displayName = GetScenarioDisplayName(name);
                bool isHovered = hoveredScenario == name;

                float cardFadeDelay = Mathf.Clamp01((menuFadeIn - 0.3f - i * 0.15f) * 3f);
                float cardOffset = (1f - cardFadeDelay) * 20f;

                Rect cardRect = new Rect(cardX, cardY + cardOffset, cardW, cardH);

                // Φόντο κάρτας (ανταποκρίνεται στο ποντίκι)
                Color bgCol = isHovered
                    ? new Color(0.12f, 0.2f, 0.3f, 0.95f)
                    : new Color(0.08f, 0.12f, 0.18f, 0.9f);
                GUI.color = new Color(bgCol.r, bgCol.g, bgCol.b, bgCol.a * cardFadeDelay);
                GUI.DrawTexture(cardRect, Texture2D.whiteTexture);

                // Χρωματιστή γραμμή αριστερά (πάλλεται αν κάνουμε hover)
                float accentAlpha = isHovered ? 0.6f + Mathf.Sin(menuTime * 4f) * 0.3f : 0.4f;
                Color accentCol = name.Contains("tachycardia")
                    ? new Color(1f, 0.4f, 0.3f, accentAlpha * cardFadeDelay)
                    : new Color(0.2f, 0.8f, 0.5f, accentAlpha * cardFadeDelay);
                GUI.color = accentCol;
                GUI.DrawTexture(new Rect(cardX, cardY + cardOffset, 4, cardH), Texture2D.whiteTexture);

                // Λάμψη στο κάτω μέρος
                if (isHovered)
                {
                    GUI.color = new Color(accentCol.r, accentCol.g, accentCol.b, 0.3f);
                    GUI.DrawTexture(new Rect(cardX, cardY + cardOffset + cardH - 1, cardW, 1), Texture2D.whiteTexture);
                }

                GUI.color = new Color(1, 1, 1, cardFadeDelay);

                // Κυκλάκι με τον αριθμό του σεναρίου
                Color badgeCol = name.Contains("tachycardia")
                    ? new Color(1f, 0.4f, 0.3f) : new Color(0.2f, 0.8f, 0.5f);
                GUI.color = new Color(badgeCol.r, badgeCol.g, badgeCol.b, 0.2f * cardFadeDelay);
                GUI.DrawTexture(new Rect(cardX + 12, cardY + cardOffset + 14, 36, 36), Texture2D.whiteTexture);
                GUI.color = new Color(1, 1, 1, cardFadeDelay);

                GUIStyle numStyle = new GUIStyle(menuButtonStyle) { fontSize = 20 };
                GUI.Label(new Rect(cardX + 12, cardY + cardOffset + 14, 36, 36), (i + 1).ToString(), numStyle);

                // Τίτλος σεναρίου
                GUI.Label(new Rect(cardX + 58, cardY + cardOffset + 10, cardW - 70, 24), displayName, menuButtonStyle);

                // Πληροφορίες δυσκολίας και περιγραφή
                string diff = name.Contains("tachycardia") ? "Προχωρημένο" : "Ενδιάμεσο";
                string desc = name.Contains("tachycardia")
                    ? "HR > 150 bpm | Αιμοδυναμική αστάθεια | Κλιμάκωση"
                    : "SpO2 < 90% | Ρύθμιση αναπνευστήρα | Τεκμηρίωση";
                GUI.color = new Color(0.55f, 0.68f, 0.78f, 0.8f * cardFadeDelay);
                GUIStyle smallStyle = new GUIStyle(menuDescStyle) { alignment = TextAnchor.MiddleLeft, fontSize = 12 };
                GUI.Label(new Rect(cardX + 58, cardY + cardOffset + 35, cardW - 70, 16), $"Δυσκολία: {diff}", smallStyle);
                GUI.Label(new Rect(cardX + 58, cardY + cardOffset + 52, cardW - 70, 16), desc, smallStyle);

                GUI.color = Color.white;

                // Τσεκάρουμε αν το ποντίκι είναι πάνω στην κάρτα
                if (cardRect.Contains(Event.current.mousePosition))
                    hoveredScenario = name;
                else if (hoveredScenario == name)
                    hoveredScenario = "";

                // Όταν πατήσουμε μια κάρτα, ξεκινάει το αντίστοιχο σενάριο
                if (GUI.Button(cardRect, "", GUIStyle.none))
                {
                    showMainMenu = false;
                    showOverlay = false;
                    menuFadeIn = 0f;
                    GameController.Instance.StartGame(name);
                }

                cardY += cardH + 10f;
            }
        }

        // Κάτω Κουμπιά (Βοήθεια / Έξοδος) 
        cardY += 15f;
        float smallBtnW = 195f;
        float smallBtnH = 40f;

        // Κουμπί Βοήθειας
        Rect helpRect = new Rect(centerX - smallBtnW - 6, cardY, smallBtnW, smallBtnH);
        bool helpHov = helpRect.Contains(Event.current.mousePosition);
        GUI.color = helpHov ? new Color(0.1f, 0.18f, 0.28f, 0.95f) : new Color(0.06f, 0.1f, 0.16f, 0.9f);
        GUI.DrawTexture(helpRect, Texture2D.whiteTexture);
        GUI.color = new Color(0.2f, 0.7f, 0.9f, helpHov ? 0.7f : 0.4f);
        GUI.DrawTexture(new Rect(helpRect.x, helpRect.y + helpRect.height - 2, helpRect.width, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(helpRect, "Βοήθεια (F1)", menuButtonStyle);
        if (GUI.Button(helpRect, "", GUIStyle.none))
        {
            showMainMenu = false;
            showOverlay = true;
            showHelp = true;
        }

        // Κουμπί Εξόδου
        Rect quitRect = new Rect(centerX + 6, cardY, smallBtnW, smallBtnH);
        bool quitHov = quitRect.Contains(Event.current.mousePosition);
        GUI.color = quitHov ? new Color(0.2f, 0.1f, 0.1f, 0.95f) : new Color(0.06f, 0.1f, 0.16f, 0.9f);
        GUI.DrawTexture(quitRect, Texture2D.whiteTexture);
        GUI.color = new Color(0.9f, 0.3f, 0.3f, quitHov ? 0.7f : 0.3f);
        GUI.DrawTexture(new Rect(quitRect.x, quitRect.y + quitRect.height - 2, quitRect.width, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(quitRect, "Έξοδος", menuButtonStyle);
        if (GUI.Button(quitRect, "", GUIStyle.none))
        {
            GameController.Instance?.QuitApplication();
        }

        // Κειμενάκι στο τέλος της οθόνης
        GUI.color = new Color(0.35f, 0.45f, 0.55f, 0.35f * fade);
        GUI.Label(new Rect(0, Screen.height - 32, Screen.width, 22),
            "Extended Reality (XR) | ICU Simulation",
            menuDescStyle);
        GUI.color = Color.white;
    }

    private void DrawMenuECG(float fade)
    {
        float ecgY = Screen.height * 0.5f;
        float w = Screen.width;
        int points = (int)(w * 0.5f);

        Color ecgCol = new Color(0.1f, 0.35f, 0.2f, 0.12f * fade);
        GUI.color = ecgCol;

        for (int i = 0; i < points - 1; i++)
        {
            float x = (float)i / points * w;
            float phase = (x / w * 3f + menuTime * 0.3f) % 1f;
            float v1 = GenerateMenuECG(phase);

            float nextX = (float)(i + 1) / points * w;
            float nextPhase = (nextX / w * 3f + menuTime * 0.3f) % 1f;
            float v2 = GenerateMenuECG(nextPhase);

            float y1 = ecgY - v1 * 40f;
            float y2 = ecgY - v2 * 40f;

            DrawMenuLine(x, y1, nextX, y2, 1.5f);
        }

        GUI.color = Color.white;
    }

    private float GenerateMenuECG(float p)
    {
        if (p < 0.10f) return Mathf.Sin(p / 0.10f * Mathf.PI) * 0.12f;
        if (p < 0.16f) return 0f;
        if (p < 0.19f) return -Mathf.Sin((p - 0.16f) / 0.03f * Mathf.PI) * 0.08f;
        if (p < 0.24f)
        {
            float t = (p - 0.19f) / 0.05f;
            return t < 0.5f ? Mathf.Lerp(-0.08f, 1f, t * 2f) : Mathf.Lerp(1f, -0.15f, (t - 0.5f) * 2f);
        }
        if (p < 0.28f) return Mathf.Lerp(-0.15f, 0f, (p - 0.24f) / 0.04f);
        if (p < 0.38f) return 0.02f;
        if (p < 0.52f) return Mathf.Sin((p - 0.38f) / 0.14f * Mathf.PI) * 0.2f;
        return 0f;
    }

    private void DrawMenuLine(float x1, float y1, float x2, float y2, float thickness)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 0.5f) return;
        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
        Matrix4x4 saved = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, new Vector2(x1, y1));
        GUI.DrawTexture(new Rect(x1, y1 - thickness * 0.5f, len, thickness), Texture2D.whiteTexture);
        GUI.matrix = saved;
    }

    private void DrawCornerMarkers(float fade)
    {
        float len = 25f;
        float t = 1.5f;
        GUI.color = new Color(0.2f, 0.6f, 0.8f, 0.2f * fade);

        // Top-left
        GUI.DrawTexture(new Rect(8, 8, len, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(8, 8, t, len), Texture2D.whiteTexture);
        // Top-right
        GUI.DrawTexture(new Rect(Screen.width - 8 - len, 8, len, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(Screen.width - 8 - t, 8, t, len), Texture2D.whiteTexture);
        // Bottom-left
        GUI.DrawTexture(new Rect(8, Screen.height - 8 - t, len, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(8, Screen.height - 8 - len, t, len), Texture2D.whiteTexture);
        // Bottom-right
        GUI.DrawTexture(new Rect(Screen.width - 8 - len, Screen.height - 8 - t, len, t), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(Screen.width - 8 - t, Screen.height - 8 - len, t, len), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    private string GetScenarioDisplayName(string id)
    {
        if (id.Contains("hypoxia"))
            return "Σενάριο 1: Διαχείριση Υποξαιμίας";
        if (id.Contains("tachycardia"))
            return "Σενάριο 2: Ταχυκαρδία & Αρρυθμία";
        return id;
    }

    private void BuildMenuStyles()
    {
        menuTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.7f, 0.92f, 1f) }
        };

        menuSubtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.45f, 0.65f, 0.8f) }
        };

        menuButtonStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.85f, 0.92f, 1f) }
        };

        menuDescStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = new Color(0.6f, 0.7f, 0.8f) }
        };
    }

    // Οθόνη χωρίς Σενάριο

    private void DrawNoScenarioScreen()
    {
        showMainMenu = true;
    }

    // Επιλογή Σεναρίου

    private void DrawScenarioSelection()
    {
        float w = 460f, h = 340f;
        Rect rect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.Box(rect, "", boxStyle);
        GUILayout.BeginArea(rect);

        GUILayout.Label("Επιλογή Σεναρίου", headerStyle);
        GUILayout.Space(8);

        ScenarioManager.Instance?.LoadAvailableScenarios();
        var scenarios = GameController.Instance?.GetAvailableScenarios();

        if (scenarios != null && scenarios.Length > 0)
        {
            foreach (string name in scenarios)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("  " + name, wrapLabelStyle, GUILayout.Width(300));
                if (GUILayout.Button("Εκκίνηση", GUILayout.Height(28), GUILayout.Width(100)))
                {
                    showScenarioSelect = false;
                    GameController.Instance.StartGame(name);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }
        }
        else
        {
            GUILayout.Label("Δεν βρέθηκαν σενάρια στο Resources/Scenarios.", wrapLabelStyle);
        }

        GUILayout.Space(12);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("← Πίσω", GUILayout.Height(28)))
        {
            showScenarioSelect = false;
        }
        if (GUILayout.Button("Αρχικό Μενού", GUILayout.Height(28)))
        {
            showScenarioSelect = false;
            showOverlay = false;
            showMainMenu = true;
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    // Πάνελ Σεναρίου (Αποφάσεις)

    private void DrawScenarioPanel()
    {
        Rect rect = new Rect(16, 16, 530, 380);
        GUI.Box(rect, "", boxStyle);
        GUILayout.BeginArea(rect);

        string title = ScenarioManager.Instance.currentScenario != null
            ? ScenarioManager.Instance.currentScenario.scenario_meta.title
            : "Σενάριο";

        GUILayout.Label(title, headerStyle);

        Node node = DecisionTreeEngine.Instance.currentNode;
        if (node == null)
        {
            GUILayout.Label("Δεν υπάρχει ενεργό node.");
            DrawControlButtons();
            GUILayout.EndArea();
            return;
        }

        string nodeLabel = node.type == "decision" ? "Απόφαση" :
                           node.type == "gate" ? "Πύλη Τεκμηρίωσης" :
                           node.type == "message" ? "Μήνυμα" :
                           node.type == "end" ? "Τέλος" : node.type;

        GUILayout.Label($"[{nodeLabel}] {node.id}", subHeaderStyle);
        GUILayout.Space(4);
        GUILayout.Label(node.text, wrapLabelStyle);
        GUILayout.Space(8);

        if (node.type == "decision" && node.options != null && node.options.Count > 0)
        {
            GUILayout.Label("Επιλογές:", subHeaderStyle);
            optionScroll = GUILayout.BeginScrollView(optionScroll, GUILayout.Height(130));
            foreach (Option option in node.options)
            {
                string hotspotHint = !string.IsNullOrEmpty(option.target_hotspot)
                    ? $" → {option.target_hotspot}" : "";
                if (GUILayout.Button($"{option.label}{hotspotHint}", GUILayout.Height(30)))
                {
                    DecisionTreeEngine.Instance.SelectOption(option);
                }
            }
            GUILayout.EndScrollView();

            if (node.timeout != null && node.timeout.seconds > 0)
            {
                GUILayout.Label($"⏱ Timeout: {node.timeout.seconds} δευτερόλεπτα", wrapLabelStyle);
            }
        }
        else if (node.type == "gate")
        {
            GUI.color = new Color(1f, 0.9f, 0.7f);
            GUILayout.Label("🛑 Συμπλήρωσε τις φόρμες στο EHR panel (δεξιά) για να προχωρήσεις.", wrapLabelStyle);
            if (!string.IsNullOrWhiteSpace(node.feedback_blocked))
                GUILayout.Label(node.feedback_blocked, wrapLabelStyle);
            GUI.color = Color.white;

            if (node.gate_requirements?.required_forms != null)
            {
                GUILayout.Space(4);
                GUILayout.Label("Απαιτούμενες φόρμες:", subHeaderStyle);
                foreach (var rf in node.gate_requirements.required_forms)
                {
                    bool done = EHRManager.Instance != null && EHRManager.Instance.IsFormCompleted(rf.form_id);
                    string icon = done ? "✅" : "⬜";
                    GUILayout.Label($"  {icon} {rf.form_id} (πεδία: {string.Join(", ", rf.fields)})", wrapLabelStyle);
                }
            }
        }
        else if (node.type == "message")
        {
            GUILayout.Label("Αυτόματη μετάβαση στο επόμενο βήμα...", wrapLabelStyle);
        }
        else if (node.type == "end")
        {
            GUILayout.Label("Το σενάριο ολοκληρώθηκε!", wrapLabelStyle);
        }

        GUILayout.Space(6);
        DrawControlButtons();
        GUILayout.EndArea();
    }

    private void DrawControlButtons()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Εκκίνηση / Επανεκκίνηση", GUILayout.Height(28)))
        {
            showScenarioSelect = true;
        }

        if (GUILayout.Button("Εξαγωγή Logs", GUILayout.Height(28)))
        {
            LoggingManager.Instance?.SaveLogsToFile("json");
            AddToast("Τα logs αποθηκεύτηκαν στο " + Application.persistentDataPath, "success");
        }

        if (GUILayout.Button("Βοήθεια (F1)", GUILayout.Height(28)))
        {
            showHelp = true;
        }

        GUILayout.EndHorizontal();
    }

    // Πάνελ Ζωτικών Λειτουργιών

    private void DrawVitalsPanel()
    {
        Rect rect = new Rect(16, 410, 270, 200);
        GUI.Box(rect, "", boxStyle);
        GUILayout.BeginArea(rect);

        GUILayout.Label("Ζωτικά Σημεία", headerStyle);

        Vitals vitals = GameStateManager.Instance != null ? GameStateManager.Instance.currentVitals : new Vitals();
        int score = GameStateManager.Instance?.currentScore ?? 0;

        bool alarm = vitals.spo2 < 90;

        if (alarm) GUI.color = new Color(1f, 0.4f, 0.4f);
        GUILayout.Label($"  HR:    {vitals.hr} bpm");
        GUILayout.Label($"  SpO2:  {vitals.spo2}%");
        if (alarm) GUI.color = Color.white;

        GUILayout.Label($"  RR:    {vitals.rr} /min");
        GUILayout.Label($"  BP:    {vitals.bp}");
        GUILayout.Label($"  Temp:  {vitals.temp:F1} °C");
        GUILayout.Space(4);
        GUILayout.Label($"  Σκορ: {score}", subHeaderStyle);

        if (alarm)
        {
            GUI.color = new Color(1f, 0.3f, 0.3f);
            GUILayout.Label("  ⚠ ΣΥΝΑΓΕΡΜΟΣ SpO2 < 90%", subHeaderStyle);
            GUI.color = Color.white;
        }

        GUILayout.EndArea();
    }

    // Πάνελ EHR (Φάκελος Ασθενή)

    private void DrawEhrPanel()
    {
        if (ScenarioManager.Instance?.currentScenario?.ehr_config?.forms == null) return;

        float panelW = 420f;
        Rect rect = new Rect(Screen.width - panelW - 16, 16, panelW, 580);
        GUI.Box(rect, "", boxStyle);
        GUILayout.BeginArea(rect);

        GUILayout.Label("Ηλεκτρονικός Φάκελος Υγείας (EHR)", headerStyle);

        if (EHRManager.Instance == null)
        {
            GUILayout.Label("EHRManager δεν είναι διαθέσιμος.");
            GUILayout.EndArea();
            return;
        }

        ehrScroll = GUILayout.BeginScrollView(ehrScroll);

        var forms = ScenarioManager.Instance.currentScenario.ehr_config.forms;
        DrawForm("assessment_form", forms.assessment_form);
        DrawForm("intervention_form", forms.intervention_form);
        DrawForm("communication_log", forms.communication_log);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawForm(string formId, FormDefinition formDef)
    {
        if (formDef == null) return;

        GUILayout.Space(8);

        bool completed = EHRManager.Instance.IsFormCompleted(formId);
        string icon = completed ? "✅" : "📝";
        GUILayout.Label($"{icon} {formDef.title}", subHeaderStyle);

        EHRManager.Instance.OpenForm(formId);

        List<string> gateFields = GetGateFieldsForForm(formId);

        foreach (string field in formDef.fields)
        {
            string existing = "";
            if (EHRManager.Instance.formFieldValues.ContainsKey(formId) &&
                EHRManager.Instance.formFieldValues[formId].ContainsKey(field))
            {
                existing = EHRManager.Instance.formFieldValues[formId][field];
            }

            bool isRequired = gateFields != null && gateFields.Contains(field);
            string fieldLabel = isRequired ? $"  {field} *" : $"  {field}";

            GUILayout.Label(fieldLabel);
            string updated = GUILayout.TextField(existing);
            if (updated != existing)
            {
                EHRManager.Instance.UpdateFieldValue(formId, field, updated);
            }
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label(completed ? "  ✅ Αποθηκεύτηκε" : "  ⬜ Εκκρεμεί", GUILayout.Width(180));

        if (GUILayout.Button(completed ? "Ενημέρωση" : "Αποθήκευση", GUILayout.Height(24)))
        {
            bool ok = EHRManager.Instance.SubmitForm(formId);
            if (ok)
                AddToast($"✅ {formDef.title}: αποθηκεύτηκε", "success");
        }
        GUILayout.EndHorizontal();
    }

    private List<string> GetGateFieldsForForm(string formId)
    {
        if (DecisionTreeEngine.Instance?.currentNode?.type != "gate") return null;
        var reqs = DecisionTreeEngine.Instance.currentNode.gate_requirements;
        if (reqs?.required_forms == null) return null;

        foreach (var rf in reqs.required_forms)
        {
            if (rf.form_id == formId) return rf.fields;
        }
        return null;
    }

    // Μίνι Χάρτης

    private void DrawMinimap()
    {
        if (Camera.main == null) return;

        float mapSize = 150f;
        float margin = 16f;
        float mapX = margin;
        float mapY = Screen.height - mapSize - 46f;

        Color prev = GUI.color;

        // Φόντο χάρτη
        GUI.color = new Color(0.03f, 0.06f, 0.1f, 0.85f);
        GUI.DrawTexture(new Rect(mapX, mapY, mapSize, mapSize), Texture2D.whiteTexture);

        // Περίγραμμα
        GUI.color = new Color(0.2f, 0.5f, 0.7f, 0.4f);
        GUI.DrawTexture(new Rect(mapX, mapY, mapSize, 1), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(mapX, mapY + mapSize - 1, mapSize, 1), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(mapX, mapY, 1, mapSize), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(mapX + mapSize - 1, mapY, 1, mapSize), Texture2D.whiteTexture);

        // Το δωμάτιο (απλό τετράγωνο)
        float roomScale = 8f;
        float roomCx = mapX + mapSize * 0.5f;
        float roomCy = mapY + mapSize * 0.5f;

        GUI.color = new Color(0.15f, 0.25f, 0.35f, 0.6f);
        float roomW = mapSize * 0.8f;
        float roomH = mapSize * 0.7f;
        GUI.DrawTexture(new Rect(roomCx - roomW * 0.5f, roomCy - roomH * 0.5f, roomW, 1), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(roomCx - roomW * 0.5f, roomCy + roomH * 0.5f, roomW, 1), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(roomCx - roomW * 0.5f, roomCy - roomH * 0.5f, 1, roomH), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(roomCx + roomW * 0.5f, roomCy - roomH * 0.5f, 1, roomH), Texture2D.whiteTexture);

        // Ζωγραφίζουμε τελίτσες για κάθε hotspot
        HotspotBase[] hotspots = FindObjectsOfType<HotspotBase>();
        foreach (HotspotBase hs in hotspots)
        {
            if (!hs.isActive) continue;
            Vector3 pos = hs.transform.position;

            float dotX = roomCx + pos.x * roomScale;
            float dotY = roomCy - pos.z * roomScale;

            dotX = Mathf.Clamp(dotX, mapX + 4, mapX + mapSize - 4);
            dotY = Mathf.Clamp(dotY, mapY + 4, mapY + mapSize - 4);

            Color dotCol = GetHotspotColor(hs.hotspotId);
            GUI.color = dotCol;
            GUI.DrawTexture(new Rect(dotX - 3, dotY - 3, 6, 6), Texture2D.whiteTexture);

            GUI.color = new Color(dotCol.r, dotCol.g, dotCol.b, 0.6f);
            GUIStyle miniLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = dotCol }
            };
            string shortName = GetShortHotspotName(hs.hotspotId);
            GUI.Label(new Rect(dotX - 25, dotY + 4, 50, 14), shortName, miniLabel);
        }

        // Η θέση του παίκτη (πάλλεται)
        Vector3 camPos = Camera.main.transform.position;
        float playerX = roomCx + camPos.x * roomScale;
        float playerY = roomCy - camPos.z * roomScale;
        playerX = Mathf.Clamp(playerX, mapX + 4, mapX + mapSize - 4);
        playerY = Mathf.Clamp(playerY, mapY + 4, mapY + mapSize - 4);

        float pulse = 0.7f + Mathf.Sin(Time.time * 4f) * 0.3f;
        GUI.color = new Color(1f, 1f, 1f, pulse);
        GUI.DrawTexture(new Rect(playerX - 4, playerY - 4, 8, 8), Texture2D.whiteTexture);

        // Προς τα που κοιτάει ο παίκτης
        Vector3 fwd = Camera.main.transform.forward;
        float dirX = playerX + fwd.x * 10f;
        float dirY = playerY - fwd.z * 10f;
        GUI.color = new Color(1f, 1f, 1f, 0.4f);
        DrawMenuLine(playerX, playerY, dirX, dirY, 1f);

        // Ταμπελάκι "MINIMAP"
        GUI.color = new Color(0.5f, 0.7f, 0.85f, 0.6f);
        GUIStyle mapLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = new Color(0.5f, 0.7f, 0.85f, 0.7f) }
        };
        GUI.Label(new Rect(mapX + 4, mapY + 2, 80, 14), "MINIMAP", mapLabel);

        GUI.color = prev;
    }

    private Color GetHotspotColor(string id)
    {
        if (id != null && id.Contains("monitor")) return new Color(0.3f, 1f, 0.4f);
        if (id != null && id.Contains("patient")) return new Color(0.3f, 0.7f, 1f);
        if (id != null && id.Contains("ventilator")) return new Color(1f, 1f, 0.3f);
        if (id != null && id.Contains("ehr")) return new Color(0.8f, 0.5f, 1f);
        if (id != null && id.Contains("call")) return new Color(1f, 0.35f, 0.35f);
        return new Color(0.6f, 0.6f, 0.6f);
    }

    private string GetShortHotspotName(string id)
    {
        if (id != null && id.Contains("monitor")) return "MON";
        if (id != null && id.Contains("patient")) return "PAT";
        if (id != null && id.Contains("ventilator")) return "VENT";
        if (id != null && id.Contains("ehr")) return "EHR";
        if (id != null && id.Contains("call")) return "CALL";
        return "?";
    }

    // Μπάρα Κατάστασης (κάτω κάτω)

    private void DrawStatusBar()
    {
        float barH = 28f;
        Rect rect = new Rect(0, Screen.height - barH, Screen.width, barH);

        Color prev = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = prev;

        float time = GameStateManager.Instance?.timeElapsed ?? 0f;
        int min = (int)(time / 60f);
        int sec = (int)(time % 60f);
        int score = GameStateManager.Instance?.currentScore ?? 0;

        string status = $"  Χρόνος: {min:00}:{sec:00}  |  Σκορ: {score}  |  Tab: Μενού  |  F1: Βοήθεια  |  E/Click: Αλληλεπίδραση  |  ESC: Παύση";

        GUIStyle barStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white }
        };
        GUI.Label(rect, status, barStyle);
    }

    // Οθόνη Βοήθειας 

    private void DrawHelpScreen()
    {
        float w = 700f, h = 520f;
        Rect rect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.Box(rect, "", boxStyle);
        GUILayout.BeginArea(rect);

        GUILayout.Label("Βοήθεια (Online Help)", headerStyle);
        GUILayout.Space(6);

        GUILayout.BeginHorizontal();

        // Αριστερά: Τα κουμπιά με τα θέματα
        GUILayout.BeginVertical(GUILayout.Width(180));
        string[] topics = { "general", "hotspots", "ehr", "decisions", "scoring", "troubleshooting" };
        string[] topicLabels = { "Γενικές Οδηγίες", "Hotspots", "EHR Σύστημα", "Λήψη Αποφάσεων", "Βαθμολόγηση", "Αντιμετώπιση" };

        for (int i = 0; i < topics.Length; i++)
        {
            bool selected = currentHelpTopic == topics[i];
            GUI.color = selected ? Color.cyan : Color.white;
            if (GUILayout.Button(topicLabels[i], GUILayout.Height(30)))
                currentHelpTopic = topics[i];
        }
        GUI.color = Color.white;
        GUILayout.EndVertical();

        // Δεξιά: Το περιεχόμενο του θέματος
        GUILayout.BeginVertical();
        helpScroll = GUILayout.BeginScrollView(helpScroll);

        string content = GetHelpContent(currentHelpTopic);
        GUILayout.Label(content, wrapLabelStyle);

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        if (GUILayout.Button("Κλείσιμο (F1)", GUILayout.Height(28)))
            showHelp = false;

        GUILayout.EndArea();
    }

    private string GetHelpContent(string topic)
    {
        switch (topic)
        {
            case "general": return
                "ΚΑΛΩΣ ΗΡΘΑΤΕ ΣΤΗΝ ΠΡΟΣΟΜΟΙΩΣΗ ΜΕΘ\n\n" +
                "Σε αυτή την εφαρμογή θα διαχειριστείτε κλινικά περιστατικά σε περιβάλλον ΜΕΘ.\n\n" +
                "ΒΑΣΙΚΟΣ ΣΤΟΧΟΣ:\n" +
                "• Αξιολογήστε τον ασθενή\n" +
                "• Λάβετε αποφάσεις με βάση τα ζωτικά σημεία\n" +
                "• Τεκμηριώστε όλες τις ενέργειές σας στο EHR\n" +
                "• Κλιμακώστε όταν χρειάζεται\n\n" +
                "ΠΛΗΚΤΡΑ:\n" +
                "• WASD: Κίνηση\n" +
                "• Mouse: Περιστροφή κάμερας\n" +
                "• E / Left Click: Αλληλεπίδραση με hotspot\n" +
                "• Tab: Εμφάνιση/Απόκρυψη μενού σεναρίου\n" +
                "• F1: Βοήθεια\n" +
                "• ESC: Παύση\n" +
                "• M: Μόνιτορ ζωτικών";

            case "hotspots": return
                "ΣΗΜΕΙΑ ΑΛΛΗΛΕΠΙΔΡΑΣΗΣ (HOTSPOTS)\n\n" +
                "1. Μόνιτορ Ζωτικών Λειτουργιών\n" +
                "   Εμφανίζει HR, SpO2, RR, BP, Temp\n\n" +
                "2. Ασθενής/Κλίνη\n" +
                "   Αξιολόγηση κατάστασης ασθενούς\n\n" +
                "3. Αναπνευστήρας\n" +
                "   Ρύθμιση FiO2 και Flow Rate\n\n" +
                "4. Τερματικό EHR\n" +
                "   Τεκμηρίωση ενεργειών\n\n" +
                "5. Κουμπί Κλήσης\n" +
                "   Κλήση ιατρού / κλιμάκωση\n\n" +
                "Στόχευσε ένα hotspot με το crosshair και πάτα E ή Click.";

            case "ehr": return
                "ΣΥΣΤΗΜΑ ΗΛΕΚΤΡΟΝΙΚΟΥ ΦΑΚΕΛΟΥ ΥΓΕΙΑΣ (EHR)\n\n" +
                "Διαθέσιμες Φόρμες:\n\n" +
                "1. Κλινική Αξιολόγηση\n" +
                "   • observation: Κύρια παρατήρηση\n" +
                "   • skin_color: Χρώμα δέρματος\n" +
                "   • consciousness: Επίπεδο συνείδησης\n\n" +
                "2. Παρεμβάσεις / Ρυθμίσεις\n" +
                "   • device: Συσκευή\n" +
                "   • fiO2_setting: Ρύθμιση οξυγόνου\n" +
                "   • flow_rate: Ρυθμός ροής\n\n" +
                "3. Ημερολόγιο Επικοινωνίας\n" +
                "   • recipient: Αποδέκτης κλήσης\n" +
                "   • reason: Λόγος\n" +
                "   • outcome: Αποτέλεσμα\n\n" +
                "⚠ GATES: Σε ορισμένα σημεία πρέπει να συμπληρώσεις\n" +
                "συγκεκριμένα πεδία (με *) για να προχωρήσεις!";

            case "decisions": return
                "ΟΔΗΓΟΣ ΛΗΨΗΣ ΑΠΟΦΑΣΕΩΝ\n\n" +
                "Βήματα Διαχείρισης:\n\n" +
                "1. ΑΞΙΟΛΟΓΗΣΗ\n" +
                "   Ελέγξτε ζωτικά, παρατηρήστε ασθενή\n\n" +
                "2. ΑΝΑΛΥΣΗ\n" +
                "   Εκτιμήστε σοβαρότητα, προτεραιότητες\n\n" +
                "3. ΠΑΡΕΜΒΑΣΗ\n" +
                "   Εφαρμόστε ενέργειες, ρυθμίστε εξοπλισμό\n\n" +
                "4. ΤΕΚΜΗΡΙΩΣΗ\n" +
                "   Καταγράψτε στο EHR\n\n" +
                "5. ΚΛΙΜΑΚΩΣΗ\n" +
                "   Καλέστε βοήθεια αν χρειάζεται\n\n" +
                "Κρίσιμοι Συναγερμοί:\n" +
                "• SpO2 < 90%: Υποξαιμία\n" +
                "• HR < 50 ή > 150: Αρρυθμία\n" +
                "• RR < 8 ή > 30: Αναπνευστική δυσχέρεια";

            case "scoring": return
                "ΣΥΣΤΗΜΑ ΒΑΘΜΟΛΟΓΗΣΗΣ\n\n" +
                "Κερδίζετε Πόντους:\n" +
                "+ Σωστή αξιολόγηση\n" +
                "+ Κατάλληλες παρεμβάσεις\n" +
                "+ Πλήρης τεκμηρίωση\n" +
                "+ Έγκαιρη κλιμάκωση\n\n" +
                "Χάνετε Πόντους:\n" +
                "- Καθυστερήσεις\n" +
                "- Λάθος επιλογές\n" +
                "- Ελλιπής τεκμηρίωση\n\n" +
                "Κλίμακα:\n" +
                "• 90-100: Άριστα\n" +
                "• 75-89: Πολύ Καλά\n" +
                "• 60-74: Καλά\n" +
                "• 50-59: Μέτρια\n" +
                "• 0-49: Χρειάζεται Βελτίωση";

            case "troubleshooting": return
                "ΑΝΤΙΜΕΤΩΠΙΣΗ ΠΡΟΒΛΗΜΑΤΩΝ\n\n" +
                "Δεν μπορώ να προχωρήσω:\n" +
                "→ Ελέγξτε αν υπάρχει ενεργό Gate.\n" +
                "→ Πατήστε Tab και συμπληρώστε το EHR.\n\n" +
                "Το μόνιτορ χτυπάει:\n" +
                "→ Χαμηλό SpO2 → Ρυθμίστε αναπνευστήρα.\n\n" +
                "Δεν ξέρω τι να επιλέξω:\n" +
                "→ Ελέγξτε τα ζωτικά σημεία.\n" +
                "→ Διαβάστε προσεκτικά το μήνυμα.\n\n" +
                "Πώς επανεκκινώ:\n" +
                "→ Tab → «Εκκίνηση / Επανεκκίνηση»\n\n" +
                "Πώς βγαίνω:\n" +
                "→ ESC → Quit";

            default: return "Δεν βρέθηκε το θέμα.";
        }
    }

    // Οθόνη Απολογισμού (Debrief)

    private void DrawDebriefScreen()
    {
        float w = 650f, h = 550f;
        Rect rect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.Box(rect, "", boxStyle);
        GUILayout.BeginArea(rect);

        GUILayout.Label("Απολογισμός (Debriefing)", headerStyle);
        GUILayout.Space(8);

        debriefScroll = GUILayout.BeginScrollView(debriefScroll);

        GUILayout.Label($"Σενάριο: {debriefData.scenarioTitle}", subHeaderStyle);
        float time = debriefData.totalTime;
        GUILayout.Label($"Συνολικός Χρόνος: {(int)(time / 60f):00}:{(int)(time % 60f):00}");
        GUILayout.Space(8);

        if (debriefData.showScore)
        {
            GUILayout.Label($"Τελικό Σκορ: {debriefData.finalScore}", headerStyle);
            GUILayout.Label($"Βαθμός: {debriefData.performanceGrade}", subHeaderStyle);
            GUILayout.Space(8);
        }

        if (debriefData.showDecisionPath && debriefData.decisionPath != null)
        {
            GUILayout.Label("Διαδρομή Αποφάσεων:", subHeaderStyle);
            for (int i = 0; i < debriefData.decisionPath.Count; i++)
            {
                GUILayout.Label($"  {i + 1}. {debriefData.decisionPath[i]}");
            }
            GUILayout.Space(8);
        }

        if (debriefData.showMissedDocumentation && debriefData.missedDocumentation != null)
        {
            if (debriefData.missedDocumentation.Count > 0)
            {
                GUI.color = new Color(1f, 0.5f, 0.5f);
                GUILayout.Label("Ελλιπής Τεκμηρίωση:", subHeaderStyle);
                foreach (string missed in debriefData.missedDocumentation)
                    GUILayout.Label($"  ✗ {missed}");
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.5f, 1f, 0.5f);
                GUILayout.Label("✅ Πλήρης τεκμηρίωση!", subHeaderStyle);
                GUI.color = Color.white;
            }
            GUILayout.Space(8);
        }

        if (debriefData.statistics != null)
        {
            GUILayout.Label("Στατιστικά:", subHeaderStyle);
            foreach (var kvp in debriefData.statistics)
                GUILayout.Label($"  {kvp.Key}: {kvp.Value}");
            GUILayout.Space(8);
        }

        if (debriefData.logExported)
        {
            GUILayout.Label($" Τα logs εξάχθηκαν στον φάκελο του παιχνιδιού", wrapLabelStyle);
        }

        GUILayout.EndScrollView();

        GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Επανεκκίνηση Σεναρίου", GUILayout.Height(30)))
        {
            showDebrief = false;
            debriefData = null;
            GameController.Instance?.RestartScenario();
        }
        if (GUILayout.Button("Νέο Σενάριο", GUILayout.Height(30)))
        {
            showDebrief = false;
            debriefData = null;
            showScenarioSelect = true;
        }
        if (GUILayout.Button("Εξαγωγή Logs", GUILayout.Height(30)))
        {
            LoggingManager.Instance?.SaveLogsToFile("json");
            LoggingManager.Instance?.SaveLogsToFile("csv");
            AddToast("Τα logs εξάχθηκαν σε JSON και CSV!", "success");
        }
        if (GUILayout.Button("✕  Έξοδος", GUILayout.Height(30)))
        {
            GameController.Instance?.QuitApplication();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    // Ρυθμίσεις Γραφικών (Styles)

    private Texture2D MakeTex(int w, int h, Color col)
    {
        Color[] pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D t = new Texture2D(w, h);
        t.SetPixels(pix);
        t.Apply();
        return t;
    }

    private void BuildStyles()
    {
        Texture2D darkBg = MakeTex(2, 2, new Color(0.08f, 0.1f, 0.14f, 0.92f));
        Texture2D panelBg = MakeTex(2, 2, new Color(0.1f, 0.12f, 0.18f, 0.9f));

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = new Color(0.7f, 0.85f, 1f) }
        };

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = new Color(0.6f, 0.9f, 1f) }
        };

        subHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = new Color(0.5f, 0.8f, 0.95f) }
        };

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(14, 14, 14, 14),
            normal = { background = darkBg }
        };

        wrapLabelStyle = new GUIStyle(GUI.skin.label)
        {
            wordWrap = true,
            fontSize = 13,
            normal = { textColor = new Color(0.85f, 0.88f, 0.92f) }
        };

        toastStyle = new GUIStyle(GUI.skin.label)
        {
            wordWrap = true,
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        toastDangerStyle = new GUIStyle(toastStyle) { normal = { textColor = new Color(1f, 0.35f, 0.35f) } };
        toastSuccessStyle = new GUIStyle(toastStyle) { normal = { textColor = new Color(0.35f, 1f, 0.5f) } };
        toastWarningStyle = new GUIStyle(toastStyle) { normal = { textColor = new Color(1f, 0.85f, 0.25f) } };
    }
}