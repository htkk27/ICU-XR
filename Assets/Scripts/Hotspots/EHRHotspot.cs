using UnityEngine;

// Hotspot για το τερματικό των ηλεκτρονικών φακέλων (e-Health)
public class EHRHotspot : HotspotBase
{
    protected override void Start()
    {
        hotspotId = "hs_ehr";
        hotspotLabel = "Τερματικό EHR (e-Health)";
        base.Start();
    }

    public override void OnHotspotClick()
    {
        base.OnHotspotClick();
        
        // Ανοιγοκλείνει το debug overlay για τα EHR
        if (ScenarioDebugOverlay.Instance != null)
        {
            bool isOpen = ScenarioDebugOverlay.Instance.showOverlay && 
                      ScenarioDebugOverlay.Instance.showEhrPanel;
        
            if (isOpen)
            {
                ScenarioDebugOverlay.Instance.showOverlay = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                ScenarioDebugOverlay.Instance.showOverlay = true;
                ScenarioDebugOverlay.Instance.showEhrPanel = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}