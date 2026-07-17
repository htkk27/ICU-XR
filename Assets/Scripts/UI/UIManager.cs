using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Διαχειρίζεται όλα τα πάνελ, τα μενού και τα κείμενα στην οθόνη
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Main UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject gameplayPanel;
    public GameObject pausePanel;
    public GameObject debriefingPanel;

    [Header("Gameplay UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    [Header("Node UI")]
    public GameObject nodeMessagePanel;
    public TextMeshProUGUI nodeMessageText;
    public GameObject optionsPanel;
    public Button optionButtonPrefab;

    [Header("Toast System")]
    public GameObject toastPrefab;
    public Transform toastContainer;
    public Queue<GameObject> activeToasts = new Queue<GameObject>();

    [Header("Hotspot-Specific Panels")]
    public GameObject monitorPanel;
    public GameObject patientAssessmentPanel;
    public GameObject ventilatorPanel;
    public GameObject ehrPanel;
    public GameObject communicationPanel;

    [Header("Monitor Panel Components")]
    public TextMeshProUGUI hrText;
    public TextMeshProUGUI spo2Text;
    public TextMeshProUGUI rrText;
    public TextMeshProUGUI bpText;
    public TextMeshProUGUI tempText;
    public Image alarmIndicator;

    [Header("Ventilator Panel Components")]
    public Slider fiO2Slider;
    public TextMeshProUGUI fiO2ValueText;
    public Slider flowRateSlider;
    public TextMeshProUGUI flowRateValueText;
    public Button applyVentilatorButton;

    [Header("EHR Components")]
    public GameObject ehrFormContainer;
    public Button assessmentFormButton;
    public Button interventionFormButton;
    public Button communicationLogButton;

    [Header("Debriefing Components")]
    public TextMeshProUGUI debriefScoreText;
    public TextMeshProUGUI debriefGradeText;
    public TextMeshProUGUI debriefPathText;
    public TextMeshProUGUI debriefMissedText;
    public Button restartButton;
    public Button mainMenuButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Ακούμε τα events του παιχνιδιού
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnScoreChanged += UpdateScore;
            GameStateManager.Instance.OnVitalsChanged += UpdateVitals;
        }

        if (DecisionTreeEngine.Instance != null)
        {
            DecisionTreeEngine.Instance.OnNodeEntered += OnNodeEntered;
            DecisionTreeEngine.Instance.OnToastMessage += ShowToastFromEngine;
        }

        // Αρχικό στήσιμο
        ShowMainMenu();
    }

    #region Πλοήγηση στα κεντρικά μενού

    public void ShowMainMenu()
    {
        HideAllPanels();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void ShowGameplay()
    {
        HideAllPanels();
        if (gameplayPanel != null) gameplayPanel.SetActive(true);
    }

    public void ShowPause()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0;
    }

    public void HidePause()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1;
    }

    private void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (debriefingPanel != null) debriefingPanel.SetActive(false);
        HideAllHotspotPanels();
    }

    private void HideAllHotspotPanels()
    {
        if (monitorPanel != null) monitorPanel.SetActive(false);
        if (patientAssessmentPanel != null) patientAssessmentPanel.SetActive(false);
        if (ventilatorPanel != null) ventilatorPanel.SetActive(false);
        if (ehrPanel != null) ehrPanel.SetActive(false);
        if (communicationPanel != null) communicationPanel.SetActive(false);
    }

    #endregion

    #region Εμφάνιση Σκορ και Ζωτικών

    private void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    private void UpdateVitals(Vitals vitals)
    {
        if (hrText != null) hrText.text = $"HR: {vitals.hr} bpm";
        if (spo2Text != null) spo2Text.text = $"SpO2: {vitals.spo2}%";
        if (rrText != null) rrText.text = $"RR: {vitals.rr} /min";
        if (bpText != null) bpText.text = $"BP: {vitals.bp}";
        if (tempText != null) tempText.text = $"Temp: {vitals.temp:F1}°C";

        // Αλλάζει το χρώμα αν έχουμε συναγερμό
        if (alarmIndicator != null)
        {
            bool alarm = vitals.spo2 < 90 || vitals.hr < 50 || vitals.hr > 150;
            alarmIndicator.color = alarm ? Color.red : Color.green;
        }
    }

    private void Update()
    {
        // Ανανέωση του χρονομέτρου
        if (timerText != null && GameStateManager.Instance != null)
        {
            int minutes = Mathf.FloorToInt(GameStateManager.Instance.timeElapsed / 60);
            int seconds = Mathf.FloorToInt(GameStateManager.Instance.timeElapsed % 60);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }

    #endregion

    #region UI για το τρέχον βήμα (Node)

    private void OnNodeEntered(Node node)
    {
        // Δείχνουμε το μήνυμα του βήματος
        if (nodeMessagePanel != null && !string.IsNullOrEmpty(node.text))
        {
            nodeMessagePanel.SetActive(true);
            nodeMessageText.text = node.text;
        }

        // Αν πρέπει να πάρουμε απόφαση, δείχνουμε τα κουμπιά
        if (node.type == "decision" && node.options != null)
        {
            ShowOptions(node.options);
        }
        else
        {
            HideOptions();
        }

        // Αν είμαστε σε gate, πετάμε ενημέρωση
        if (node.type == "gate" && !string.IsNullOrEmpty(node.description))
        {
            ShowToast(node.description, "info");
        }
    }

    private void ShowOptions(List<Option> options)
    {
        if (optionsPanel == null || optionButtonPrefab == null) return;

        // Καθαρίζουμε τα παλιά κουμπιά επιλογών
        foreach (Transform child in optionsPanel.transform)
        {
            Destroy(child.gameObject);
        }

        optionsPanel.SetActive(true);

        // Φτιάχνουμε τα νέα κουμπιά
        foreach (var option in options)
        {
            Button btn = Instantiate(optionButtonPrefab, optionsPanel.transform);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = option.label;
            
            // Προσοχή εδώ: κρατάμε την επιλογή για να δουλέψει σωστά το event
            Option capturedOption = option;
            btn.onClick.AddListener(() => OnOptionSelected(capturedOption));
        }
    }

    private void HideOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
    }

    private void OnOptionSelected(Option option)
    {
        DecisionTreeEngine.Instance?.SelectOption(option);
    }

    #endregion

    #region Σύστημα Μηνυμάτων (Toasts)

    public void ShowToast(string message, string style = "info")
    {
        if (toastPrefab == null || toastContainer == null) return;

        GameObject toast = Instantiate(toastPrefab, toastContainer);
        
        // Βάζουμε το κείμενο στο toast
        TextMeshProUGUI toastText = toast.GetComponentInChildren<TextMeshProUGUI>();
        if (toastText != null)
        {
            toastText.text = message;
        }

        // Αλλάζουμε το χρώμα ανάλογα με τον τύπο (πχ success, danger)
        Image toastImage = toast.GetComponent<Image>();
        if (toastImage != null)
        {
            switch (style)
            {
                case "success":
                    toastImage.color = new Color(0.2f, 0.8f, 0.2f);
                    break;
                case "warning":
                    toastImage.color = new Color(1f, 0.8f, 0f);
                    break;
                case "danger":
                case "error":
                    toastImage.color = new Color(0.9f, 0.2f, 0.2f);
                    break;
                default: // info
                    toastImage.color = new Color(0.2f, 0.6f, 1f);
                    break;
            }
        }

        activeToasts.Enqueue(toast);
        StartCoroutine(RemoveToastAfterDelay(toast, 3f));
    }

    private void ShowToastFromEngine(string message)
    {
        ShowToast(message, "info");
    }

    private IEnumerator RemoveToastAfterDelay(GameObject toast, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (activeToasts.Count > 0 && activeToasts.Peek() == toast)
        {
            activeToasts.Dequeue();
        }
        
        Destroy(toast);
    }

    #endregion

    #region Επεξηγήσεις (Tooltips) στο ποντίκι

    public void ShowTooltip(string text)
    {
        if (tooltipPanel != null && tooltipText != null)
        {
            tooltipPanel.SetActive(true);
            tooltipText.text = text;
            
            // Το tooltip ακολουθεί το ποντίκι
            tooltipPanel.transform.position = Input.mousePosition;
        }
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    #endregion

    #region Πάνελ για τα διάφορα αντικείμενα (Μόνιτορ, EHR κλπ)

    public void ShowMonitorPanel()
    {
        HideAllHotspotPanels();
        if (monitorPanel != null)
        {
            monitorPanel.SetActive(true);
            UpdateVitals(GameStateManager.Instance?.currentVitals ?? new Vitals());
        }
    }

    public void ShowPatientAssessmentPanel()
    {
        HideAllHotspotPanels();
        if (patientAssessmentPanel != null)
        {
            patientAssessmentPanel.SetActive(true);
        }
    }

    public void ShowVentilatorPanel(VentilatorHotspot ventilator)
    {
        HideAllHotspotPanels();
        if (ventilatorPanel != null)
        {
            ventilatorPanel.SetActive(true);
            
            // Σετάρουμε τις μπάρες (sliders) του αναπνευστήρα
            if (fiO2Slider != null)
            {
                fiO2Slider.value = ventilator.currentFiO2;
                fiO2Slider.onValueChanged.AddListener(OnFiO2Changed);
            }
            
            if (flowRateSlider != null)
            {
                flowRateSlider.value = ventilator.currentFlowRate;
                flowRateSlider.onValueChanged.AddListener(OnFlowRateChanged);
            }

            if (applyVentilatorButton != null)
            {
                applyVentilatorButton.onClick.RemoveAllListeners();
                applyVentilatorButton.onClick.AddListener(() => ApplyVentilatorSettings(ventilator));
            }
        }
    }

    private void OnFiO2Changed(float value)
    {
        if (fiO2ValueText != null)
        {
            fiO2ValueText.text = $"{value:F0}%";
        }
    }

    private void OnFlowRateChanged(float value)
    {
        if (flowRateValueText != null)
        {
            flowRateValueText.text = $"{value:F0} L/min";
        }
    }

    private void ApplyVentilatorSettings(VentilatorHotspot ventilator)
    {
        if (fiO2Slider != null)
        {
            ventilator.SetFiO2((int)fiO2Slider.value);
        }
        
        if (flowRateSlider != null)
        {
            ventilator.SetFlowRate((int)flowRateSlider.value);
        }

        ShowToast("✅ Οι ρυθμίσεις του αναπνευστήρα εφαρμόστηκαν", "success");
        
        if (ventilatorPanel != null)
        {
            ventilatorPanel.SetActive(false);
        }
    }

    public void ShowEHRUI()
    {
        if (ehrPanel != null)
        {
            ehrPanel.SetActive(true);
        }
    }

    public void HideEHRUI()
    {
        if (ehrPanel != null)
        {
            ehrPanel.SetActive(false);
        }
    }

    public void ShowCommunicationPanel()
    {
        HideAllHotspotPanels();
        if (communicationPanel != null)
        {
            communicationPanel.SetActive(true);
        }
    }

    public void OnHotspotInteraction(HotspotBase hotspot)
    {
        Debug.Log($"UI received hotspot interaction: {hotspot.hotspotId}");

        if (hotspot == null || DecisionTreeEngine.Instance == null)
        {
            return;
        }

        Node currentNode = DecisionTreeEngine.Instance.currentNode;
        if (currentNode == null)
        {
            return;
        }

        if (currentNode.type == "decision" && currentNode.options != null)
        {
            foreach (Option option in currentNode.options)
            {
                if (option.target_hotspot == hotspot.hotspotId)
                {
                    DecisionTreeEngine.Instance.SelectOption(option);
                    return;
                }
            }

            ShowToast($"Για αυτό το βήμα δεν χρησιμοποιείται το hotspot: {hotspot.hotspotLabel}", "warning");
            return;
        }

        if (currentNode.type == "gate" && currentNode.gate_requirements != null)
        {
            if (currentNode.gate_requirements.target_hotspot == hotspot.hotspotId)
            {
                if (!string.IsNullOrEmpty(currentNode.feedback_blocked))
                {
                    ShowToast(currentNode.feedback_blocked, "warning");
                }
                return;
            }
        }
    }

    #endregion

    #region Οπτικά Εφέ

    public void ApplyVisualEffect(string target, string state)
    {
        // Ρίχνει εφέ σε συγκεκριμένα αντικείμενα
        if (target == "hs_monitor")
        {
            MonitorHotspot monitor = HotspotManager.Instance?.monitorHotspot;
            if (monitor != null)
            {
                monitor.ApplyVisualEffect(state);
            }
        }
    }

    #endregion

    #region Οθόνη Απολογισμού

    public void ShowDebriefingPanel(DebriefData debriefData)
    {
        HideAllPanels();
        
        if (debriefingPanel != null)
        {
            debriefingPanel.SetActive(true);
            
            // Γεμίζουμε τα δεδομένα του debriefing
            if (debriefScoreText != null && debriefData.showScore)
            {
                debriefScoreText.text = $"Τελικό Score: {debriefData.finalScore}";
            }

            if (debriefGradeText != null)
            {
                debriefGradeText.text = $"Αξιολόγηση: {debriefData.performanceGrade}";
            }

            if (debriefPathText != null && debriefData.showDecisionPath)
            {
                debriefPathText.text = "Διαδρομή Αποφάσεων:\n" + string.Join(" → ", debriefData.decisionPath);
            }

            if (debriefMissedText != null && debriefData.showMissedDocumentation)
            {
                if (debriefData.missedDocumentation.Count > 0)
                {
                    debriefMissedText.text = "⚠️ Παραλήφθηκε τεκμηρίωση:\n" + string.Join("\n", debriefData.missedDocumentation);
                }
                else
                {
                    debriefMissedText.text = "✅ Όλη η τεκμηρίωση συμπληρώθηκε σωστά!";
                }
            }

            // Σετάρουμε τα κουμπιά για επανεκκίνηση ή μενού
            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(ShowMainMenu);
            }
        }
    }

    private void OnRestartClicked()
    {
        ScenarioManager.Instance?.ResetScenario();
        ShowGameplay();
        DecisionTreeEngine.Instance?.StartScenario();
    }

    #endregion
}