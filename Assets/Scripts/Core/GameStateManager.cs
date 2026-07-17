using System;
using System.Collections.Generic;
using UnityEngine;

// Κρατάει το state (πόσο σκορ έχουμε, vitals, χρόνο και διάφορα flags του σεναρίου)
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Current Game State")]
    public int currentScore;
    public float timeElapsed;
    public Dictionary<string, bool> flags = new Dictionary<string, bool>();
    public Vitals currentVitals = new Vitals();
    public List<string> activeHotspots = new List<string>();
    public bool monitorAlert = false;

    [Header("Critical Thresholds")]
    public int criticalSpo2Threshold = 70;
    public int criticalHrHighThreshold = 200;
    public int criticalHrLowThreshold = 30;
    public bool patientDied = false;

    // Events
    public event Action<int> OnScoreChanged;
    public event Action<Vitals> OnVitalsChanged;
    public event Action<string, bool> OnFlagChanged;
    public event Action OnPatientDied;

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

    // Σετάρει την αρχική κατάσταση μόλις φορτώσει το σενάριο
    public void InitializeState(InitialState initialState)
    {
        currentScore = initialState.current_score;
        timeElapsed = initialState.time_elapsed;
        patientDied = false;
        
        // Αρχικοποίηση flags
        flags.Clear();
        flags["assessment_complete"] = initialState.flags.assessment_complete;
        flags["oxygen_adjusted"] = initialState.flags.oxygen_adjusted;
        flags["documentation_1_complete"] = initialState.flags.documentation_1_complete;
        flags["escalation_complete"] = initialState.flags.escalation_complete;
        flags["documentation_2_complete"] = initialState.flags.documentation_2_complete;
        
        // Αρχικοποίηση vitals
        currentVitals = new Vitals
        {
            hr = initialState.vitals.hr,
            spo2 = initialState.vitals.spo2,
            rr = initialState.vitals.rr,
            bp = initialState.vitals.bp,
            temp = initialState.vitals.temp
        };
        
        // Αρχικοποίηση UI state
        activeHotspots = new List<string>(initialState.ui.active_hotspots);
        monitorAlert = initialState.ui.monitor_alert;
        
        OnVitalsChanged?.Invoke(currentVitals);
        OnScoreChanged?.Invoke(currentScore);
    }

    // Αλλάζει το σκορ του παίκτη
    public void UpdateScore(int delta)
    {
        currentScore = Mathf.Clamp(currentScore + delta, 0, 100);
        OnScoreChanged?.Invoke(currentScore);
        Debug.Log($"Score updated: {currentScore} (delta: {delta})");
    }

    // Αλλάζει τα vitals (πχ ρίχνει τους σφυγμούς)
    public void UpdateVitals(Dictionary<string, object> updates)
    {
        foreach (var kvp in updates)
        {
            switch (kvp.Key)
            {
                case "hr":
                    currentVitals.hr = Convert.ToInt32(kvp.Value);
                    break;
                case "spo2":
                    currentVitals.spo2 = Convert.ToInt32(kvp.Value);
                    break;
                case "rr":
                    currentVitals.rr = Convert.ToInt32(kvp.Value);
                    break;
                case "bp":
                    currentVitals.bp = kvp.Value.ToString();
                    break;
                case "temp":
                    currentVitals.temp = Convert.ToSingle(kvp.Value);
                    break;
            }
        }
        
        OnVitalsChanged?.Invoke(currentVitals);
        Debug.Log($"Vitals updated: HR={currentVitals.hr}, SpO2={currentVitals.spo2}");

        CheckCriticalVitals();
    }

    // Τσεκάρει αν τα πράγματα ξέφυγαν εκτός των ορίων που έχουμε θέσει (ώστε να τερματίσει το game)
    private void CheckCriticalVitals()
    {
        if (patientDied) return;

        bool isCritical = currentVitals.spo2 < criticalSpo2Threshold ||
                           currentVitals.hr > criticalHrHighThreshold ||
                           currentVitals.hr < criticalHrLowThreshold;

        if (isCritical)
        {
            TriggerPatientDeath();
        }
    }

    // Game over: Μηδενίζει το σκορ και ρίχνει την ειδοποίηση θανάτου
    private void TriggerPatientDeath()
    {
        patientDied = true;
        currentScore = 0;
        OnScoreChanged?.Invoke(currentScore);

        Debug.LogWarning("Patient died - critical vitals reached!");

        LoggingManager.Instance?.LogEvent("PATIENT_DEATH", new Dictionary<string, object>
        {
            { "hr", currentVitals.hr },
            { "spo2", currentVitals.spo2 },
            { "bp", currentVitals.bp }
        });

        OnPatientDied?.Invoke();
        DecisionTreeEngine.Instance?.TriggerPatientDeathEnd();
    }

    // Γυρνάει διακόπτες/flags στο σενάριο
    public void UpdateFlags(Dictionary<string, object> updates)
    {
        foreach (var kvp in updates)
        {
            bool value = Convert.ToBoolean(kvp.Value);
            flags[kvp.Key] = value;
            OnFlagChanged?.Invoke(kvp.Key, value);
            Debug.Log($"Flag updated: {kvp.Key} = {value}");
        }
    }

    // Διαβάζει αν ένα flag είναι true
    public bool GetFlag(string flagName)
    {
        return flags.ContainsKey(flagName) && flags[flagName];
    }

    // Βοηθητικό για να παίρνουμε εύκολα την τιμή ενός vital
    public object GetVitalValue(string vitalName)
    {
        switch (vitalName)
        {
            case "hr": return currentVitals.hr;
            case "spo2": return currentVitals.spo2;
            case "rr": return currentVitals.rr;
            case "bp": return currentVitals.bp;
            case "temp": return currentVitals.temp;
            default: return null;
        }
    }

    // Καθαρίζει τα πάντα για νέο παιχνίδι
    public void ResetState()
    {
        currentScore = 0;
        timeElapsed = 0;
        flags.Clear();
        currentVitals = new Vitals();
        activeHotspots.Clear();
        monitorAlert = false;
        patientDied = false;
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;
    }
}