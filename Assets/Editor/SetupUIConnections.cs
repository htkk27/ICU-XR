using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

// Editor script για σύνδεση UI components

public class SetupUIConnections : EditorWindow
{
    [MenuItem("ICU XR/Setup UI Panels")]
    public static void SetupUI()
    {
        // Βρες το Canvas
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found! Please create MainCanvas first.");
            return;
        }

        GameObject canvasGO = canvas.gameObject;

        // Δημιούργησε τα βασικά UI panels
        
        // 1. Message Panel (για node messages) - Μικρότερο και στο κάτω μέρος
        GameObject messagePanel = CreatePanel("MessagePanel", canvasGO, new Vector2(800, 120), new Vector2(0, -350));
        messagePanel.SetActive(false); // Ξεκινάει κρυμμένο
        GameObject messageText = CreateText("MessageText", messagePanel, "", 16, TextAlignmentOptions.Center);
        // Κάνε το text να μην μπλοκάρει clicks
        messageText.GetComponent<TextMeshProUGUI>().raycastTarget = false;

        // 2. Toast Container (για notifications)
        GameObject toastContainer = CreatePanel("ToastContainer", canvasGO, new Vector2(400, 100), new Vector2(0, -200));

        // 3. Options Panel (για decision buttons)
        GameObject optionsPanel = CreatePanel("OptionsPanel", canvasGO, new Vector2(600, 150), new Vector2(0, -100));
        
        // 4. HUD Panel (για score, time, vitals)
        GameObject hudPanel = CreatePanel("HUDPanel", canvasGO, new Vector2(300, 100), new Vector2(-450, 250));
        CreateText("ScoreText", hudPanel, "Score: 0", 16, TextAlignmentOptions.Left);
        CreateText("TimerText", hudPanel, "Time: 0:00", 16, TextAlignmentOptions.Left);

        // 5. Monitor Panel (για vital signs)
        GameObject monitorPanel = CreatePanel("MonitorPanel", canvasGO, new Vector2(400, 300), new Vector2(0, 0));
        monitorPanel.SetActive(false); // Ξεκινάει κρυμμένο
        CreateText("MonitorTitle", monitorPanel, "🖥️ VITAL SIGNS MONITOR", 20, TextAlignmentOptions.Center);
        CreateText("HR_Text", monitorPanel, "HR: -- bpm", 16, TextAlignmentOptions.Left);
        CreateText("SpO2_Text", monitorPanel, "SpO2: -- %", 16, TextAlignmentOptions.Left);
        CreateText("BP_Text", monitorPanel, "BP: --/-- mmHg", 16, TextAlignmentOptions.Left);
        CreateText("RR_Text", monitorPanel, "RR: -- /min", 16, TextAlignmentOptions.Left);

        // 6. EHR Panel (για electronic health records)
        GameObject ehrPanel = CreatePanel("EHRPanel", canvasGO, new Vector2(500, 400), new Vector2(0, 0));
        ehrPanel.SetActive(false); // Ξεκινάει κρυμμένο
        CreateText("EHRTitle", ehrPanel, "📋 ELECTRONIC HEALTH RECORD", 20, TextAlignmentOptions.Center);

        // 7. Help Panel
        GameObject helpPanel = CreatePanel("HelpPanel", canvasGO, new Vector2(700, 500), new Vector2(0, 0));
        helpPanel.SetActive(false);
        CreateText("HelpTitle", helpPanel, "❓ ΒΟΗΘΕΙΑ (F1 to close)", 22, TextAlignmentOptions.Center);
        CreateText("HelpContent", helpPanel, GetHelpText(), 14, TextAlignmentOptions.Left);

        Debug.Log("✅ UI Panels created successfully!");
        Debug.Log("🔗 Now connect these in UIManager Inspector manually:");
        Debug.Log("   - MessagePanel → nodeMessagePanel");
        Debug.Log("   - MessageText → nodeMessageText");
        Debug.Log("   - ToastContainer → toastContainer");
        Debug.Log("   - MonitorPanel → monitorPanel");
        Debug.Log("   - EHRPanel → ehrFormContainer");
        Debug.Log("   - HelpPanel → helpPanel");
    }

    private static GameObject CreatePanel(string name, GameObject parent, Vector2 size, Vector2 position)
    {
        // Έλεγξε αν υπάρχει ήδη
        Transform existing = parent.transform.Find(name);
        if (existing != null)
        {
            Debug.LogWarning($"{name} already exists. Reusing...");
            return existing.gameObject;
        }

        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent.transform, false);

        // RectTransform
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Image (background)
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.7f); // Σκούρο semi-transparent
        img.raycastTarget = false; // ΔΕΝ μπλοκάρει clicks!

        // CanvasGroup για fade effects
        CanvasGroup cg = panel.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false; // ΔΕΝ μπλοκάρει clicks!

        return panel;
    }

    private static GameObject CreateText(string name, GameObject parent, string content, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent.transform, false);

        RectTransform rt = textGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(10, 10);
        rt.offsetMax = new Vector2(-10, -10);

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;

        return textGO;
    }

    private static string GetHelpText()
    {
        return @"🏥 ICU XR - ΟΔΗΓΙΕΣ ΧΡΗΣΗΣ

🖱️ ΕΛΕΓΧΟΙ:
• Click σε Hotspots για αλληλεπίδραση
• F1 - Εμφάνιση/Απόκρυψη βοήθειας
• ESC - Παύση

🎯 HOTSPOTS:
🔵 Monitor - Παρακολούθηση vital signs
🟡 Patient - Εξέταση ασθενούς
🟢 Ventilator - Ρύθμιση αναπνευστήρα
🔵 EHR Terminal - Ηλεκτρονικό φάκελο
🔴 Call Button - Κλήση βοήθειας

📋 ΣΤΟΧΟΣ:
Ακολουθήστε το scenario και λάβετε τις σωστές αποφάσεις!";
    }
}