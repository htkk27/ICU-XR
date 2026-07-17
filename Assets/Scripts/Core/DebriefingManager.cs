using System.Collections.Generic;
using UnityEngine;

// Ελέγχει την οθόνη του debriefing όταν τελειώνει το σενάριο
public class DebriefingManager : MonoBehaviour
{
    public static DebriefingManager Instance { get; private set; }

    [Header("Debrief Data")]
    public DebriefData currentDebriefData;

    // Events
    public System.Action<DebriefData> OnDebriefingReady;

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

    // Φτιάχνει τα δεδομένα και εμφανίζει το debriefing panel
    public void ShowDebriefing(DebriefConfig config)
    {
        currentDebriefData = GenerateDebriefData(config);
        OnDebriefingReady?.Invoke(currentDebriefData);
        
        // Εμφάνιση UI debriefing panel
        UIManager.Instance?.ShowDebriefingPanel(currentDebriefData);
    }

    // Μαζεύει σκορ, στατιστικά και λάθη για το τελικό report
    private DebriefData GenerateDebriefData(DebriefConfig config)
    {
        DebriefData data = new DebriefData();

        // Βαθμολογία
        if (config.show_score)
        {
            data.finalScore = GameStateManager.Instance?.currentScore ?? 0;
            data.showScore = true;
        }

        // Πορεία αποφάσεων
        if (config.show_decision_path)
        {
            data.decisionPath = LoggingManager.Instance?.GetDecisionPath() ?? new List<string>();
            data.showDecisionPath = true;
        }

        // Ξεχασμένες φόρμες (EHR)
        if (config.highlight_missed_docs)
        {
            data.missedDocumentation = CheckMissedDocumentation();
            data.showMissedDocumentation = true;
        }

        // Στατιστικά session
        data.statistics = LoggingManager.Instance?.GetSessionStatistics() ?? new Dictionary<string, object>();

        // Γενικές πληροφορίες
        data.scenarioTitle = ScenarioManager.Instance?.currentScenario?.scenario_meta?.title ?? "Unknown";
        data.totalTime = GameStateManager.Instance?.timeElapsed ?? 0;

        // Εξαγωγή αρχείου αν χρειάζεται
        if (config.export_log)
        {
            LoggingManager.Instance?.SaveLogsToFile("json");
            data.logExported = true;
        }

        // Τελικός χαρακτηρισμός
        data.performanceGrade = CalculatePerformanceGrade(data.finalScore);

        return data;
    }

    // Τσεκάρει αν ο παίκτης ξέχασε να συμπληρώσει κάποια φόρμα (EHR)
    private List<string> CheckMissedDocumentation()
    {
        List<string> missed = new List<string>();

        // Κοιτάμε αν άναψαν ποτέ αυτά τα flags
        if (GameStateManager.Instance != null)
        {
            if (!GameStateManager.Instance.GetFlag("documentation_1_complete"))
            {
                missed.Add("Αρχική Αξιολόγηση & Παρέμβαση");
            }
            if (!GameStateManager.Instance.GetFlag("documentation_2_complete"))
            {
                missed.Add("Καταγραφή Επικοινωνίας");
            }
        }

        return missed;
    }

    // Βγάζει τον τελικό χαρακτηρισμό με βάση το σκορ
    private string CalculatePerformanceGrade(int score)
    {
        if (score >= 90) return "Άριστα";
        if (score >= 75) return "Πολύ Καλά";
        if (score >= 60) return "Καλά";
        if (score >= 50) return "Μέτρια";
        if (score >= 20) return "Χρειάζεται Βελτίωση";
        return "Αποτυχία";
    }
}

// Κρατάει όλη την πληροφορία που θα δείξουμε στο τέλος
[System.Serializable]
public class DebriefData
{
    public string scenarioTitle;
    public float totalTime;
    
    public bool showScore;
    public int finalScore;
    public string performanceGrade;
    
    public bool showDecisionPath;
    public List<string> decisionPath;
    
    public bool showMissedDocumentation;
    public List<string> missedDocumentation;
    
    public Dictionary<string, object> statistics;
    
    public bool logExported;
}