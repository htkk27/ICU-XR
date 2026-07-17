using System;
using System.Collections.Generic;
using UnityEngine;

// Ένας fallback JSON parser αν δεν υπάρχει το Newtonsoft εγκατεστημένο
public static class SimpleJsonParser
{
    // Φορτώνει το σενάριο
    public static ScenarioData ParseScenario(string json)
    {
        try
        {
            #if NEWTONSOFT_JSON
            return Newtonsoft.Json.JsonConvert.DeserializeObject<ScenarioData>(json);
            #else
            return JsonUtility.FromJson<ScenarioData>(json);
            #endif
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON Parse Error: {e.Message}");
            return null;
        }
    }

    // Φορτώνει ένα απλό Dictionary
    public static Dictionary<string, object> ParseDictionary(string json)
    {
        Dictionary<string, object> result = new Dictionary<string, object>();
        
        // Απλή υλοποίηση, τη μεγαλώνουμε αν χρειαστεί
        try
        {
            #if NEWTONSOFT_JSON
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            #else
            Debug.LogWarning("Using basic dictionary parsing - some features may be limited");
            return result;
            #endif
        }
        catch
        {
            return result;
        }
    }
}