using UnityEngine;

// Hotspot για το κουμπί που καλεί βοήθεια
public class CallButtonHotspot : HotspotBase
{
    [Header("Call Button Specific")]
    public GameObject callLight;
    public AudioSource callSound;
    public bool callActive = false;

    protected override void Start()
    {
        hotspotId = "hs_call";
        hotspotLabel = "Κουμπί Κλήσης";
        base.Start();
    }

    public override void OnHotspotClick()
    {
        base.OnHotspotClick(); // Κρατάμε τη βασική συμπεριφορά για την επιλογή
        
        // Μόνο οπτικοακουστικό feedback εδώ, δεν ανοίγουμε κάποιο UI panel
        if (callLight != null) callLight.SetActive(true);
        if (callSound != null) callSound.Play();
        
        callActive = true;
    }

    // Ανάβει το λαμπάκι, παίζει ήχο και πετάει το πάνελ επικοινωνίας
    private void ActivateCall()
    {
        callActive = true;

        if (callLight != null) callLight.SetActive(true);
        if (callSound != null) callSound.Play();

        UIManager.Instance?.ShowCommunicationPanel();
        Debug.Log("Emergency call activated");
    }

    // Σβήνει το λαμπάκι και ακυρώνει την κλήση
    public void DeactivateCall()
    {
        callActive = false;
        if (callLight != null) callLight.SetActive(false);
    }
}