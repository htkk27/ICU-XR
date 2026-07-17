using System.Collections.Generic;
using UnityEngine;

// Ο κεντρικός manager που ελέγχει όλα τα hotspots στη σκηνή
public class HotspotManager : MonoBehaviour
{
    public static HotspotManager Instance { get; private set; }

    [Header("Registered Hotspots")]
    public Dictionary<string, HotspotBase> hotspots = new Dictionary<string, HotspotBase>();

    [Header("Hotspot References")]
    public MonitorHotspot monitorHotspot;
    public PatientHotspot patientHotspot;
    public VentilatorHotspot ventilatorHotspot;
    public EHRHotspot ehrHotspot;
    public CallButtonHotspot callButtonHotspot;

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

    // Προσθέτει ένα νέο hotspot στο dictionary
    public void RegisterHotspot(HotspotBase hotspot)
    {
        if (!hotspots.ContainsKey(hotspot.hotspotId))
        {
            hotspots[hotspot.hotspotId] = hotspot;
            CacheTypedReference(hotspot);
            Debug.Log($"Hotspot registered: {hotspot.hotspotId}");
        }
    }

    // Το αφαιρεί από το dictionary
    public void UnregisterHotspot(HotspotBase hotspot)
    {
        if (hotspots.ContainsKey(hotspot.hotspotId))
        {
            hotspots.Remove(hotspot.hotspotId);
        }
    }

    // Ψάχνει να βρει hotspot με βάση το ID του
    public HotspotBase GetHotspot(string hotspotId)
    {
        if (hotspots.ContainsKey(hotspotId))
        {
            return hotspots[hotspotId];
        }
        return null;
    }

    // Ενεργοποιεί/απενεργοποιεί συγκεκριμένο hotspot
    public void SetHotspotActive(string hotspotId, bool active)
    {
        HotspotBase hotspot = GetHotspot(hotspotId);
        if (hotspot != null)
        {
            hotspot.SetActive(active);
        }
    }

    // Αφήνει ενεργά μόνο όσα του δώσουμε στη λίστα
    public void SetActiveHotspots(List<string> activeHotspotIds)
    {
        foreach (var kvp in hotspots)
        {
            bool shouldBeActive = activeHotspotIds.Contains(kvp.Key);
            kvp.Value.SetActive(shouldBeActive);
        }
    }

    // Ανοίγει όλα τα hotspots
    public void EnableAllHotspots()
    {
        foreach (var kvp in hotspots)
        {
            kvp.Value.SetActive(true);
        }
    }

    // Απενεργοποιεί όλα τα hotspots
    public void DisableAllHotspots()
    {
        foreach (var kvp in hotspots)
        {
            kvp.Value.SetActive(false);
        }
    }

    // Κρατάει έτοιμα τα references για γρήγορη πρόσβαση
    private void CacheTypedReference(HotspotBase hotspot)
    {
        if (hotspot is MonitorHotspot monitor)
        {
            monitorHotspot = monitor;
        }
        else if (hotspot is PatientHotspot patient)
        {
            patientHotspot = patient;
        }
        else if (hotspot is VentilatorHotspot ventilator)
        {
            ventilatorHotspot = ventilator;
        }
        else if (hotspot is EHRHotspot ehr)
        {
            ehrHotspot = ehr;
        }
        else if (hotspot is CallButtonHotspot callButton)
        {
            callButtonHotspot = callButton;
        }
    }
}