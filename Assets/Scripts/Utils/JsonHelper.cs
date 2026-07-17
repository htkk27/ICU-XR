using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Βοηθητικά εργαλεία για να δουλεύουμε πιο εύκολα με JSON αρχεία
public static class JsonHelper
{
    // Μετατρέπει ένα JSON string σε C# object (με ασφάλεια για να μην κρασάρει αν κάτι πάει στραβά)
    public static T DeserializeObject<T>(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"JSON Deserialization failed: {e.Message}");
            return default(T);
        }
    }

    // Κάνει το C# object ένα JSON string
    public static string SerializeObject(object obj, bool indented = false)
    {
        try
        {
            Formatting formatting = indented ? Formatting.Indented : Formatting.None;
            return JsonConvert.SerializeObject(obj, formatting);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"JSON Serialization failed: {e.Message}");
            return null;
        }
    }

    // Διαβάζει ένα δυναμικό Dictionary από JToken (Newtonsoft)
    public static Dictionary<string, object> ParseDictionary(JToken token)
    {
        if (token == null) return new Dictionary<string, object>();

        try
        {
            return token.ToObject<Dictionary<string, object>>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}