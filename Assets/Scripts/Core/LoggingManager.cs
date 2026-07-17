using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

// Κρατάει αρχείο (logs) με το τι κάνει ο παίκτης (για exports σε JSON/CSV)
public class LoggingManager : MonoBehaviour
{
    public static LoggingManager Instance { get; private set; }

    [Header("Logging Configuration")]
    public bool loggingEnabled = true;
    public List<string> loggedEvents = new List<string>
    {
        "NODE_ENTER", "HOTSPOT_INTERACTION", "OPTION_SELECTED", 
        "EHR_SUBMIT", "VITALS_CHANGE", "SCORE_CHANGE"
    };

    [Header("Log Data")]
    public List<LogEntry> logEntries = new List<LogEntry>();
    public DateTime sessionStartTime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            sessionStartTime = DateTime.Now;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Προσθέτει μια νέα εγγραφή στο log
    public void LogEvent(string eventType, Dictionary<string, object> data = null)
    {
        if (!loggingEnabled) return;
        if (!loggedEvents.Contains(eventType)) return;

        LogEntry entry = new LogEntry
        {
            timestamp = DateTime.Now,
            eventType = eventType,
            gameTime = GameStateManager.Instance?.timeElapsed ?? 0,
            score = GameStateManager.Instance?.currentScore ?? 0,
            data = data ?? new Dictionary<string, object>()
        };

        // Καταγραφή vitals τη στιγμή της ενέργειας
        if (GameStateManager.Instance != null)
        {
            entry.vitals = new Dictionary<string, object>
            {
                { "hr", GameStateManager.Instance.currentVitals.hr },
                { "spo2", GameStateManager.Instance.currentVitals.spo2 },
                { "rr", GameStateManager.Instance.currentVitals.rr },
                { "bp", GameStateManager.Instance.currentVitals.bp },
                { "temp", GameStateManager.Instance.currentVitals.temp }
            };
        }

        logEntries.Add(entry);
        Debug.Log($"[LOG] {eventType}: {JsonConvert.SerializeObject(data)}");
    }

    // Βγάζει τα data σε JSON
    public string ExportToJson()
    {
        SessionLog sessionLog = new SessionLog
        {
            sessionStartTime = sessionStartTime,
            sessionEndTime = DateTime.Now,
            scenarioId = ScenarioManager.Instance?.currentScenario?.scenario_meta?.id,
            scenarioTitle = ScenarioManager.Instance?.currentScenario?.scenario_meta?.title,
            finalScore = GameStateManager.Instance?.currentScore ?? 0,
            totalGameTime = GameStateManager.Instance?.timeElapsed ?? 0,
            entries = logEntries
        };

        return JsonConvert.SerializeObject(sessionLog, Formatting.Indented);
    }

    // Βγάζει τα data σε CSV
    public string ExportToCsv()
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Timestamp,EventType,GameTime,Score,Data");

        foreach (var entry in logEntries)
        {
            string dataStr = JsonConvert.SerializeObject(entry.data).Replace(",", ";");
            csv.AppendLine($"{entry.timestamp:yyyy-MM-dd HH:mm:ss},{entry.eventType},{entry.gameTime:F2},{entry.score},\"{dataStr}\"");
        }

        return csv.ToString();
    }

    // Αποθηκεύει το αρχείο στον δίσκο (στο persistent path του Unity)
    public void SaveLogsToFile(string format = "json")
    {
        string filename = $"ICU_Log_{DateTime.Now:yyyyMMdd_HHmmss}.{format}";
        string path = Path.Combine(Application.dataPath, "..", filename);

        string content = format == "json" ? ExportToJson() : ExportToCsv();
        File.WriteAllText(path, content);

        Debug.Log($"Logs saved to: {path}");
    }

    // Μηδενίζει τα logs (χρήσιμο στα restarts)
    public void ClearLogs()
    {
        logEntries.Clear();
        sessionStartTime = DateTime.Now;
    }

    // Δίνει τη σειρά των αποφάσεων που πήρε ο παίκτης
    public List<string> GetDecisionPath()
    {
        List<string> path = new List<string>();
        foreach (var entry in logEntries)
        {
            if (entry.eventType == "NODE_ENTER" && entry.data.ContainsKey("node_id"))
            {
                path.Add(entry.data["node_id"].ToString());
            }
        }
        return path;
    }

    // Φτιάχνει συγκεντρωτικά στατιστικά για την οθόνη του debrief
    public Dictionary<string, object> GetSessionStatistics()
    {
        int totalDecisions = 0;
        int hotspotInteractions = 0;
        int ehrSubmissions = 0;

        foreach (var entry in logEntries)
        {
            switch (entry.eventType)
            {
                case "OPTION_SELECTED":
                    totalDecisions++;
                    break;
                case "HOTSPOT_INTERACTION":
                    hotspotInteractions++;
                    break;
                case "EHR_SUBMIT":
                    ehrSubmissions++;
                    break;
            }
        }

        return new Dictionary<string, object>
        {
            { "total_decisions", totalDecisions },
            { "hotspot_interactions", hotspotInteractions },
            { "ehr_submissions", ehrSubmissions },
            { "total_events", logEntries.Count },
            { "session_duration_seconds", (DateTime.Now - sessionStartTime).TotalSeconds }
        };
    }
}

// Το μοντέλο για ένα συγκεκριμένο συμβάν στο log
[Serializable]
public class LogEntry
{
    public DateTime timestamp;
    public string eventType;
    public float gameTime;
    public int score;
    public Dictionary<string, object> data;
    public Dictionary<string, object> vitals;
}

// Όλο το πακέτο του log
[Serializable]
public class SessionLog
{
    public DateTime sessionStartTime;
    public DateTime sessionEndTime;
    public string scenarioId;
    public string scenarioTitle;
    public int finalScore;
    public float totalGameTime;
    public List<LogEntry> entries;
}