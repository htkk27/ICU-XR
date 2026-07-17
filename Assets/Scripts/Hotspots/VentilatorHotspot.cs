using System.Collections.Generic;
using UnityEngine;

// Hotspot για το μηχάνημα του αναπνευστήρα (ventilator)
public class VentilatorHotspot : HotspotBase
{
    [Header("Ventilator Specific")]
    public int currentFiO2 = 40; // Σε τι ποσοστό είναι το οξυγόνο
    public int currentFlowRate = 10; // Πόσο δυνατά δίνει αέρα (L/min)

    protected override void Start()
    {
        hotspotId = "hs_ventilator";
        hotspotLabel = "Αναπνευστήρας";
        base.Start();
    }

    public override void OnHotspotClick()
    {
        base.OnHotspotClick();
        
        // Ανοίγει το UI του αναπνευστήρα για ρυθμίσεις
        UIManager.Instance?.ShowVentilatorPanel(this);
    }

    // Αλλαγή του FiO2
    public void SetFiO2(int value)
    {
        currentFiO2 = Mathf.Clamp(value, 21, 100);
        Debug.Log($"FiO2 set to: {currentFiO2}%");
        
        // Logs
        LoggingManager.Instance?.LogEvent("VENTILATOR_ADJUSTMENT", new Dictionary<string, object>
        {
            { "parameter", "FiO2" },
            { "value", currentFiO2 }
        });
    }

    // Αλλαγή του ρυθμού ροής
    public void SetFlowRate(int value)
    {
        currentFlowRate = Mathf.Clamp(value, 1, 20);
        Debug.Log($"Flow rate set to: {currentFlowRate} L/min");
        
        LoggingManager.Instance?.LogEvent("VENTILATOR_ADJUSTMENT", new Dictionary<string, object>
        {
            { "parameter", "FlowRate" },
            { "value", currentFlowRate }
        });
    }
}