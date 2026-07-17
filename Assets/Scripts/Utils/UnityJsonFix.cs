using System;
using System.Collections.Generic;
using UnityEngine;

// Wrapper classes για Unity JsonUtility compatibility
// Το JsonUtility δεν υποστηρίζει Dictionary<string, object>

namespace ICU.Json
{
    // Απλό ζευγάρι Key-Value που μπορεί να σωθεί
    [Serializable]
    public class SerializableKeyValue
    {
        public string key;
        public string value;

        public SerializableKeyValue(string k, string v)
        {
            key = k;
            value = v;
        }
    }

    // Σώζουμε το Dictionary σαν List
    [Serializable]
    public class SerializableDictionary
    {
        public List<SerializableKeyValue> items = new List<SerializableKeyValue>();

        public Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (var item in items)
            {
                dict[item.key] = item.value;
            }
            return dict;
        }

        public void FromDictionary(Dictionary<string, object> dict)
        {
            items.Clear();
            foreach (var kvp in dict)
            {
                items.Add(new SerializableKeyValue(kvp.Key, kvp.Value?.ToString() ?? ""));
            }
        }
    }

    // Εργαλεία για μετατροπές από string σε Dictionary και αντιστρόφως
    public static class DictionaryConverter
    {
        public static Dictionary<string, object> StringToDict(string str)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            
            if (string.IsNullOrEmpty(str)) return result;
            
            // Απλό σπάσιμο string.
            string[] pairs = str.Split(',');
            foreach (var pair in pairs)
            {
                string[] kv = pair.Split(':');
                if (kv.Length == 2)
                {
                    result[kv[0].Trim()] = kv[1].Trim();
                }
            }
            
            return result;
        }

        public static string DictToString(Dictionary<string, object> dict)
        {
            if (dict == null || dict.Count == 0) return "";
            
            List<string> pairs = new List<string>();
            foreach (var kvp in dict)
            {
                pairs.Add($"{kvp.Key}:{kvp.Value}");
            }
            
            return string.Join(",", pairs);
        }
    }
}