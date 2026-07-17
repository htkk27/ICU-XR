using System;
using System.Linq;
using UnityEngine;

// Αρχικοποιεί αυτόματα τα hotspots ψάχνοντας συγκεκριμένα objects στη σκηνή
public class SceneHotspotBootstrap : MonoBehaviour
{
    public static SceneHotspotBootstrap Instance { get; private set; }

    [Header("Auto Setup")]
    public bool autoSetupOnStart = true;
    public bool verboseLogs = true;

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
        if (autoSetupOnStart)
        {
            EnsureHotspotsInScene();
        }
    }

    // Ψάχνει τα game objects και τους συνδέει με τα σωστά scripts 
    public void EnsureHotspotsInScene()
    {
        SetupHotspot<MonitorHotspot>("HeartScanner Variant", "Monitor");
        SetupHotspot<PatientHotspot>("OperatinTable Variant", "Patient");
        SetupHotspot<VentilatorHotspot>("Vantilator Variant", "Ventilator");

        // Ψάχνουμε το EHR μηχάνημα με διάφορα ονόματα
        GameObject ehrObject = FindByAnyName("Machine Variant", "MachineA Variant", "MachineB Variant");
        if (ehrObject != null)
        {
            EnsureComponent<EHRHotspot>(ehrObject, "EHR");
            EnsureCollider(ehrObject);
        }

        // Ψάχνουμε το κουμπί με διάφορα ονόματα
        GameObject callButton = FindByAnyName("CallButton", "export3dcoat", "redcallbutton");
        if (callButton != null)
        {
            EnsureComponent<CallButtonHotspot>(callButton, "Call Button");
            EnsureCollider(callButton);
        }
        else
        {
            Log("Δεν βρέθηκε CallButton στη σκηνή.");
        }
    }

    // Helper για να σετάρει ένα συγκεκριμένο hotspot
    private void SetupHotspot<T>(string objectName, string label) where T : HotspotBase
    {
        GameObject target = FindByAnyName(objectName);
        if (target == null)
        {
            Log($"Δεν βρέθηκε object για {label}: {objectName}");
            return;
        }

        EnsureComponent<T>(target, label);
        EnsureCollider(target);
    }

    // Εξασφαλίζει ότι το object έχει το απαραίτητο component, αλλιώς το προσθέτει
    private void EnsureComponent<T>(GameObject target, string label) where T : Component
    {
        if (target.GetComponent<T>() == null)
        {
            target.AddComponent<T>();
            Log($"Προστέθηκε {typeof(T).Name} στο {target.name} ({label})");
        }
    }

    // Βάζει BoxCollider αν δεν υπάρχει ήδη
    private void EnsureCollider(GameObject target)
    {
        Collider existingCollider = target.GetComponent<Collider>();
        if (existingCollider != null) return;

        Renderer renderer = target.GetComponentInChildren<Renderer>();
        BoxCollider collider = target.AddComponent<BoxCollider>();

        if (renderer != null)
        {
            // Κεντράρει και δίνει μέγεθος στο collider βάσει του mesh renderer
            Bounds bounds = renderer.bounds;
            Vector3 localCenter = target.transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = target.transform.InverseTransformVector(bounds.size);
            collider.center = localCenter;
            collider.size = new Vector3(
                Mathf.Abs(localSize.x),
                Mathf.Abs(localSize.y),
                Mathf.Abs(localSize.z));
        }

        Log($"Προστέθηκε BoxCollider στο {target.name}");
    }

    private void SetColor(GameObject obj, Color color)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r != null) r.material.color = color;
    }

    // Helper που ψάχνει object δοκιμάζοντας πολλαπλά πιθανά ονόματα
    private GameObject FindByAnyName(params string[] names)
    {
        Transform[] allTransforms = FindObjectsOfType<Transform>(true);

        foreach (string targetName in names)
        {
            Transform match = allTransforms.FirstOrDefault(t => string.Equals(t.name, targetName, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                return match.gameObject;
            }
        }

        return null;
    }

    private void Log(string message)
    {
        if (verboseLogs)
        {
            Debug.Log("[SceneHotspotBootstrap] " + message);
        }
    }
}