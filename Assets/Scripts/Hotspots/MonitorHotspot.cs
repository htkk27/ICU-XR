using UnityEngine;

// Hotspot για την οθόνη με τα ζωτικά του ασθενή (monitor)
public class MonitorHotspot : HotspotBase
{
    [Header("Monitor Specific")]
    public bool isAlarming = false;
    public GameObject alarmLight;
    public AudioSource alarmSound;

    protected override void Start()
    {
        hotspotId = "hs_monitor";
        hotspotLabel = "Μόνιτορ Ζωτικών Λειτουργιών";
        base.Start();

        // Παρακολουθούμε τις αλλαγές στα vitals
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnVitalsChanged += OnVitalsChanged;
        }
    }

    protected override void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnVitalsChanged -= OnVitalsChanged;
        }
        base.OnDestroy();
    }

    public override void OnHotspotClick()
    {
        base.OnHotspotClick();
        
        // Ανοίγουμε το UI του monitor
        UIManager.Instance?.ShowMonitorPanel();
        VitalsMonitorDisplay.Instance?.Toggle();
    }

    // Trigger όταν χτυπήσει update στα ζωτικά
    private void OnVitalsChanged(Vitals vitals)
    {
        CheckAlarms(vitals);
    }

    // Τσεκάρει τα όρια για να ρίξει συναγερμό αν κάτι δεν πάει καλά
    private void CheckAlarms(Vitals vitals)
    {
        bool shouldAlarm = false;

        // Critical thresholds (Όρια που χτυπάει το alarm)
        if (vitals.spo2 < 90) shouldAlarm = true;
        if (vitals.hr < 50 || vitals.hr > 150) shouldAlarm = true;
        if (vitals.rr < 8 || vitals.rr > 30) shouldAlarm = true;

        SetAlarmState(shouldAlarm);
    }

    // Ανάβει/Σβήνει τον συναγερμό (φως και ήχος)
    public void SetAlarmState(bool alarming)
    {
        isAlarming = alarming;

        if (alarmLight != null)
        {
            alarmLight.SetActive(alarming);
        }

        if (alarmSound != null)
        {
            if (alarming && !alarmSound.isPlaying)
            {
                alarmSound.loop = true;
                alarmSound.Play();
            }
            else if (!alarming && alarmSound.isPlaying)
            {
                alarmSound.Stop();
            }
        }
    }

    // Ρίχνει forced effects ανάλογα με το κανόνα του σεναρίου
    public void ApplyVisualEffect(string effectState)
    {
        switch (effectState)
        {
            case "blinking_red":
                SetAlarmState(true);
                break;
            case "normal":
                SetAlarmState(false);
                break;
        }
    }
}