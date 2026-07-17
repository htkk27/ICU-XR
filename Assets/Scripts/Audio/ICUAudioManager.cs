using UnityEngine;

// Παράγει τους ήχους της ΜΕΘ (μηχανήματα, μόνιτορ) μέσα από κώδικα (synth) χωρίς να χρειάζεται MP3
public class ICUAudioManager : MonoBehaviour
{
    public static ICUAudioManager Instance { get; private set; }

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 0.6f;
    [Range(0f, 1f)] public float alarmVolume = 0.8f;
    [Range(0f, 1f)] public float heartbeatVolume = 0.35f;
    [Range(0f, 1f)] public float clickVolume = 0.5f;
    [Range(0f, 1f)] public float ventilatorVolume = 0.15f;

    [Header("Alarm")]
    public float alarmFrequency = 880f;
    public float alarmBeepDuration = 0.15f;
    public float alarmPauseDuration = 0.35f;

    [Header("Heartbeat")]
    public float heartbeatFrequency = 440f;
    public float heartbeatBeepDuration = 0.06f;

    private AudioSource alarmSource;
    private AudioSource heartbeatSource;
    private AudioSource clickSource;
    private AudioSource ventilatorSource;

    private AudioClip alarmClip;
    private AudioClip heartbeatClip;
    private AudioClip clickClip;
    private AudioClip ventilatorClip;

    private float heartbeatTimer;
    private float heartbeatInterval = 0.857f; // ~70 bpm
    private bool alarmActive;
    private float alarmTimer;
    private bool alarmBeepOn;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        CreateAudioSources();
        GenerateClips();
    }

    // Φτιάχνει τα game objects που θα παίζουν τους ήχους
    private void CreateAudioSources()
    {
        alarmSource = CreateSource("AlarmSource");
        heartbeatSource = CreateSource("HeartbeatSource");
        clickSource = CreateSource("ClickSource");
        ventilatorSource = CreateSource("VentilatorSource");
        ventilatorSource.loop = true;
    }

    private AudioSource CreateSource(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; 
        return src;
    }

    // Κατασκευάζει τους ήχους
    private void GenerateClips()
    {
        alarmClip = GenerateTone(alarmFrequency, alarmBeepDuration, ToneShape.Square);
        heartbeatClip = GenerateTone(heartbeatFrequency, heartbeatBeepDuration, ToneShape.Sine);
        clickClip = GenerateClick(0.04f);
        ventilatorClip = GenerateVentilator(2f);
    }

    private enum ToneShape { Sine, Square }

    // Συνθέτει έναν απλό τόνο (μπιπ) μαθηματικά
    private AudioClip GenerateTone(float freq, float duration, ToneShape shape)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f;
            float fadeIn = 0.005f;
            float fadeOut = 0.01f;
            if (t < fadeIn) envelope = t / fadeIn;
            else if (t > duration - fadeOut) envelope = (duration - t) / fadeOut;
            envelope = Mathf.Clamp01(envelope);

            float wave;
            if (shape == ToneShape.Square)
                wave = Mathf.Sin(2f * Mathf.PI * freq * t) > 0 ? 0.5f : -0.5f;
            else
                wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f;

            samples[i] = wave * envelope;
        }

        AudioClip clip = AudioClip.Create("Tone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Συνθέτει ένα ήχο "κλικ" UI
    private AudioClip GenerateClick(float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 80f);
            float noise = (Random.value * 2f - 1f) * 0.3f;
            float tone = Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.5f;
            samples[i] = (tone + noise) * envelope;
        }

        AudioClip clip = AudioClip.Create("Click", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Συνθέτει έναν ήχο αναπνευστήρα (χρησιμοποιώντας θόρυβο και Perlin Noise)
    private AudioClip GenerateVentilator(float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float breathCycle = Mathf.Sin(2f * Mathf.PI * 0.2f * t);
            float envelope = Mathf.Abs(breathCycle) * 0.5f + 0.1f;
            float noise = (Mathf.PerlinNoise(t * 200f, 0f) * 2f - 1f) * 0.4f;
            float hiss = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.1f;
            samples[i] = (noise + hiss) * envelope;
        }

        AudioClip clip = AudioClip.Create("Ventilator", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void Update()
    {
        if (GameController.Instance == null || !GameController.Instance.isGameRunning)
        {
            StopAllSounds();
            return;
        }

        bool lowSpo2 = GameStateManager.Instance != null && GameStateManager.Instance.currentVitals.spo2 < 90;
        UpdateAlarm(lowSpo2);
        UpdateHeartbeat();
        UpdateVentilator();
    }

    private void StopAllSounds()
    {
        if (alarmActive) { alarmActive = false; alarmSource.Stop(); }
        if (ventilatorSource.isPlaying) ventilatorSource.Stop();
        heartbeatTimer = 0f;
    }

    // Χειρίζεται το πότε αρχίζει και πότε σταματά το αλάρμ
    private void UpdateAlarm(bool shouldAlarm)
    {
        if (shouldAlarm && !alarmActive)
        {
            alarmActive = true;
            alarmTimer = 0f;
            alarmBeepOn = false;
        }
        else if (!shouldAlarm && alarmActive)
        {
            alarmActive = false;
            alarmSource.Stop();
        }

        if (!alarmActive) return;

        alarmTimer -= Time.deltaTime;
        if (alarmTimer <= 0f)
        {
            alarmBeepOn = !alarmBeepOn;
            if (alarmBeepOn)
            {
                alarmSource.clip = alarmClip;
                alarmSource.volume = alarmVolume * masterVolume;
                alarmSource.Play();
                alarmTimer = alarmBeepDuration;
            }
            else
            {
                alarmTimer = alarmPauseDuration;
            }
        }
    }

    // Συγχρονίζει το μπιπ της καρδιάς με τους σφυγμούς του GameStateManager
    private void UpdateHeartbeat()
    {
        if (GameStateManager.Instance == null) return;
        if (!GameController.Instance?.isGameRunning == true) return;

        int hr = GameStateManager.Instance.currentVitals.hr;
        if (hr <= 0) return;

        heartbeatInterval = 60f / hr;
        heartbeatTimer -= Time.deltaTime;

        if (heartbeatTimer <= 0f)
        {
            heartbeatSource.clip = heartbeatClip;
            heartbeatSource.volume = heartbeatVolume * masterVolume;
            heartbeatSource.pitch = Mathf.Lerp(0.8f, 1.3f, Mathf.InverseLerp(50f, 150f, hr));
            heartbeatSource.Play();
            heartbeatTimer = heartbeatInterval;
        }
    }

    // Ανοιγοκλείνει τον ήχο του αναπνευστήρα
    private void UpdateVentilator()
    {
        bool shouldPlay = GameController.Instance?.isGameRunning == true;

        if (shouldPlay && !ventilatorSource.isPlaying)
        {
            ventilatorSource.clip = ventilatorClip;
            ventilatorSource.volume = ventilatorVolume * masterVolume;
            ventilatorSource.Play();
        }
        else if (!shouldPlay && ventilatorSource.isPlaying)
        {
            ventilatorSource.Stop();
        }

        if (ventilatorSource.isPlaying)
        {
            ventilatorSource.volume = ventilatorVolume * masterVolume;
        }
    }

    public void PlayClickSound()
    {
        if (clickSource == null || clickClip == null) return;
        clickSource.clip = clickClip;
        clickSource.volume = clickVolume * masterVolume;
        clickSource.Play();
    }

    public void PlayAlertBeep()
    {
        if (alarmSource == null || alarmClip == null) return;
        alarmSource.clip = alarmClip;
        alarmSource.volume = alarmVolume * masterVolume;
        alarmSource.PlayOneShot(alarmClip);
    }
}