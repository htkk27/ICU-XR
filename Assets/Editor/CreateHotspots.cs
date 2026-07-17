using UnityEngine;
using UnityEditor;

// Editor script για γρήγορη δημιουργία hotspots
public class CreateHotspots : EditorWindow
{
    [MenuItem("ICU XR/Create 5 Hotspots")]
    public static void CreateAllHotspots()
    {
        // Βρες ή δημιούργησε το ICU_Room
        GameObject icuRoom = GameObject.Find("ICU_Room");
        if (icuRoom == null)
        {
            Debug.LogError("ICU_Room not found! Please create it first.");
            return;
        }

        // 1. Monitor Hotspot
        CreateHotspot("HS_Monitor", "Monitor", new Vector3(-2, 1.5f, 2), icuRoom, typeof(MonitorHotspot), Color.cyan);
        
        // 2. Patient Hotspot
        CreateHotspot("HS_Patient", "Patient/Bed", new Vector3(0, 0.5f, 0), icuRoom, typeof(PatientHotspot), Color.yellow);
        
        // 3. Ventilator Hotspot
        CreateHotspot("HS_Ventilator", "Ventilator", new Vector3(2, 1, 2), icuRoom, typeof(VentilatorHotspot), Color.green);
        
        // 4. EHR Terminal Hotspot
        CreateHotspot("HS_EHR", "EHR Terminal", new Vector3(-3, 1, -2), icuRoom, typeof(EHRHotspot), Color.blue);
        
        // 5. Call Button Hotspot
        CreateHotspot("HS_CallButton", "Call Button", new Vector3(3, 1.2f, -2), icuRoom, typeof(CallButtonHotspot), Color.red);

        Debug.Log("✅ Created 5 Hotspots successfully!");
        
        // Register with HotspotManager
        HotspotManager hotspotManager = GameObject.FindObjectOfType<HotspotManager>();
        if (hotspotManager != null)
        {
            Debug.Log("✅ HotspotManager found. Hotspots will auto-register on Play.");
        }
    }

    private static void CreateHotspot(string objectName, string label, Vector3 position, GameObject parent, System.Type scriptType, Color color)
    {
        // Έλεγξε αν υπάρχει ήδη
        Transform existing = parent.transform.Find(objectName);
        if (existing != null)
        {
            Debug.LogWarning($"{objectName} already exists. Skipping...");
            return;
        }

        // Δημιούργησε το GameObject
        GameObject hotspot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hotspot.name = objectName;
        hotspot.transform.parent = parent.transform;
        hotspot.transform.localPosition = position;
        hotspot.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        // Πρόσθεσε το script
        Component scriptComponent = hotspot.AddComponent(scriptType);

        // Ρύθμισε το material
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        hotspot.GetComponent<Renderer>().material = mat;

        // Βεβαιώσου ότι έχει Collider
        BoxCollider collider = hotspot.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = hotspot.AddComponent<BoxCollider>();
        }
        collider.isTrigger = false; 

        // Πρόσθεσε tag για εύκολη αναγνώριση
        if (hotspot.tag == "Untagged")
        {
            hotspot.tag = "Untagged"; 
        }

        Debug.Log($"✅ Created {objectName} at {position}");
    }
}