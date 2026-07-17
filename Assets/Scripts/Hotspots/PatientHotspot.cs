using UnityEngine;

// Hotspot για το κρεβάτι και τον ίδιο τον ασθενή
public class PatientHotspot : HotspotBase
{
    [Header("Patient Specific")]
    public Animator patientAnimator;
    
    protected override void Start()
    {
        hotspotId = "hs_patient";
        hotspotLabel = "Ασθενής/Κλίνη";
        base.Start();
    }

    public override void OnHotspotClick()
    {
        base.OnHotspotClick();
        
        // Ανοίγει το UI για να εξετάσουμε τον ασθενή
        UIManager.Instance?.ShowPatientAssessmentPanel();
    }

    // Αλλάζει το animation (π.χ. τον βάζει να ανασαίνει βαριά)
    public void SetPatientState(string state)
    {
        if (patientAnimator != null)
        {
            patientAnimator.SetTrigger(state);
        }
    }
}