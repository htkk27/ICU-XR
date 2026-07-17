using UnityEngine;

public class VitalsMonitorDisplay : MonoBehaviour
{
    public static VitalsMonitorDisplay Instance { get; private set; }

    [Header("Display")]
    public bool showMonitor = false;
    public KeyCode toggleKey = KeyCode.M;

    [Header("Panel")]
    public float panelWidth = 520f;
    public float panelHeight = 380f;

    private GUIStyle headerStyle;
    private GUIStyle valueStyle;
    private GUIStyle labelStyle;
    private GUIStyle unitStyle;
    private GUIStyle boxStyle;
    private Texture2D bgTex;

    private float time;
    private float[] ecgBuffer;
    private float[] spo2Buffer;
    private float[] rrBuffer;
    private int bufferSize = 200;
    private int writeIndex;

    private float ecgPhase;
    private float spo2Phase;
    private float rrPhase;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        ecgBuffer = new float[bufferSize];
        spo2Buffer = new float[bufferSize];
        rrBuffer = new float[bufferSize];
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showMonitor = !showMonitor;
        }

        time += Time.deltaTime;
        UpdateWaveforms();
    }

    public void Show() { showMonitor = true; }
    public void Hide() { showMonitor = false; }
    public void Toggle() { showMonitor = !showMonitor; }

    private void UpdateWaveforms()
    {
        Vitals v = GameStateManager.Instance != null
            ? GameStateManager.Instance.currentVitals
            : new Vitals { hr = 72, spo2 = 98, rr = 16, bp = "120/80", temp = 36.6f };

        float hrFreq = v.hr / 60f;
        float rrFreq = v.rr / 60f;

        ecgPhase += Time.deltaTime * hrFreq;
        spo2Phase += Time.deltaTime * hrFreq;
        rrPhase += Time.deltaTime * rrFreq;

        ecgBuffer[writeIndex] = GenerateECG(ecgPhase);
        spo2Buffer[writeIndex] = GenerateSpO2(spo2Phase);
        rrBuffer[writeIndex] = GenerateRespiration(rrPhase);

        writeIndex = (writeIndex + 1) % bufferSize;
    }

    private float GenerateECG(float phase)
    {
        float p = phase % 1f;

        // Κύμα P
        if (p < 0.10f)
            return Mathf.Sin(p / 0.10f * Mathf.PI) * 0.12f;
        // Διάστημα PR
        if (p < 0.16f)
            return 0f;
        // Κύμα Q
        if (p < 0.19f)
            return -Mathf.Sin((p - 0.16f) / 0.03f * Mathf.PI) * 0.08f;
        // Κύμα R (η μεγάλη κορυφή)
        if (p < 0.24f)
        {
            float t = (p - 0.19f) / 0.05f;
            return t < 0.5f
                ? Mathf.Lerp(-0.08f, 1f, t * 2f)
                : Mathf.Lerp(1f, -0.15f, (t - 0.5f) * 2f);
        }
        // Κύμα S
        if (p < 0.28f)
            return Mathf.Lerp(-0.15f, 0f, (p - 0.24f) / 0.04f);
        // Διάστημα ST
        if (p < 0.38f)
            return 0.02f;
        // Κύμα T
        if (p < 0.52f)
            return Mathf.Sin((p - 0.38f) / 0.14f * Mathf.PI) * 0.2f;
        // Βασική γραμμή (baseline)
        return 0f;
    }

    private float GenerateSpO2(float phase)
    {
        float p = phase % 1f;

        // Συστολική άνοδος
        if (p < 0.15f)
            return Mathf.Sin(p / 0.15f * Mathf.PI * 0.5f) * 0.8f;
        // Δικροτική εγκοπή και πτώση
        if (p < 0.30f)
        {
            float t = (p - 0.15f) / 0.15f;
            float descent = 0.8f * (1f - t * 0.6f);
            float notch = -Mathf.Sin(t * Mathf.PI) * 0.08f;
            return descent + notch;
        }
        // Διαστολική πτώση
        if (p < 0.80f)
        {
            float t = (p - 0.30f) / 0.50f;
            return Mathf.Lerp(0.35f, 0.02f, t * t);
        }
        return 0.02f;
    }

    private float GenerateRespiration(float phase)
    {
        float p = phase % 1f;
        return Mathf.Sin(p * Mathf.PI * 2f) * 0.4f;
    }

    private void OnGUI()
    {
        if (!showMonitor) return;

        if (headerStyle == null) BuildStyles();

        float x = (Screen.width - panelWidth) * 0.5f;
        float y = (Screen.height - panelHeight) * 0.5f;
        Rect panel = new Rect(x, y, panelWidth, panelHeight);

        GUI.Box(panel, "", boxStyle);

        Vitals v = GameStateManager.Instance != null
            ? GameStateManager.Instance.currentVitals
            : new Vitals { hr = 72, spo2 = 98, rr = 16, bp = "120/80", temp = 36.6f };

        bool alarm = v.spo2 < 90;

        float headerH = 30f;
        GUI.Label(new Rect(x + 12, y + 6, panelWidth - 24, headerH),
            "ICU Patient Monitor", headerStyle);

        float waveY = y + 36f;
        float waveH = 70f;
        float waveW = panelWidth * 0.62f;
        float numX = x + waveW + 10f;
        float numW = panelWidth - waveW - 22f;

        // ECG
        DrawWaveform(new Rect(x + 10, waveY, waveW, waveH), ecgBuffer,
            new Color(0.2f, 1f, 0.3f), -0.3f, 1.1f);
        DrawVitalValue(new Rect(numX, waveY, numW, waveH),
            "HR", v.hr.ToString(), "bpm", new Color(0.2f, 1f, 0.3f));

        waveY += waveH + 6f;

        // SpO2
        Color spo2Col = alarm ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.8f, 1f);
        DrawWaveform(new Rect(x + 10, waveY, waveW, waveH), spo2Buffer,
            spo2Col, -0.1f, 0.9f);
        DrawVitalValue(new Rect(numX, waveY, numW, waveH),
            "SpO2", v.spo2.ToString() + "%", "", spo2Col);

        waveY += waveH + 6f;

        // RR
        DrawWaveform(new Rect(x + 10, waveY, waveW, waveH), rrBuffer,
            new Color(1f, 1f, 0.3f), -0.5f, 0.5f);
        DrawVitalValue(new Rect(numX, waveY, numW, waveH),
            "RR", v.rr.ToString(), "/min", new Color(1f, 1f, 0.3f));

        waveY += waveH + 10f;

        // Μπάρα πληροφοριών κάτω κάτω
        float barH = 40f;
        Rect barRect = new Rect(x + 10, waveY, panelWidth - 20, barH);
        Color prev = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.DrawTexture(barRect, Texture2D.whiteTexture);
        GUI.color = prev;

        GUI.Label(new Rect(barRect.x + 10, barRect.y + 4, 120, 18), "BP:", labelStyle);
        GUI.Label(new Rect(barRect.x + 38, barRect.y + 4, 100, 18), v.bp, valueStyle);

        GUI.Label(new Rect(barRect.x + 150, barRect.y + 4, 120, 18), "Temp:", labelStyle);
        GUI.Label(new Rect(barRect.x + 200, barRect.y + 4, 100, 18),
            $"{v.temp:F1} °C", valueStyle);

        if (alarm)
        {
            float blink = Mathf.Sin(Time.time * 8f) > 0 ? 1f : 0f;
            GUI.color = new Color(1f, 0.2f, 0.2f, blink);
            GUI.Label(new Rect(barRect.x + 320, barRect.y + 2, 160, 36),
                "ALARM", headerStyle);
            GUI.color = Color.white;
        }

        // Οδηγία για το πώς να κλείσει
        GUI.Label(new Rect(x + 12, y + panelHeight - 22, 200, 20),
            "[E] Κλείσιμο", labelStyle);
    }

    private void DrawWaveform(Rect rect, float[] buffer, Color color, float minVal, float maxVal)
    {
        Color prev = GUI.color;

        // Μαύρο φόντο οθόνης
        GUI.color = new Color(0.03f, 0.05f, 0.03f, 0.95f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        // Οι γραμμούλες του πλέγματος (grid)
        GUI.color = new Color(0.1f, 0.15f, 0.1f, 0.7f);
        int gridLines = 4;
        for (int i = 1; i < gridLines; i++)
        {
            float gy = rect.y + rect.height * i / gridLines;
            GUI.DrawTexture(new Rect(rect.x, gy, rect.width, 1), Texture2D.whiteTexture);
        }
        for (int i = 1; i < 8; i++)
        {
            float gx = rect.x + rect.width * i / 8f;
            GUI.DrawTexture(new Rect(gx, rect.y, 1, rect.height), Texture2D.whiteTexture);
        }

        // Η ίδια η κυματομορφή
        GUI.color = color;
        float range = maxVal - minVal;
        if (range < 0.001f) range = 1f;

        int pixelCount = Mathf.Min((int)rect.width, bufferSize);
        int readStart = (writeIndex - pixelCount + bufferSize) % bufferSize;

        for (int i = 0; i < pixelCount - 1; i++)
        {
            int idx0 = (readStart + i) % bufferSize;
            int idx1 = (readStart + i + 1) % bufferSize;

            float v0 = Mathf.Clamp01((buffer[idx0] - minVal) / range);
            float v1 = Mathf.Clamp01((buffer[idx1] - minVal) / range);

            float y0 = rect.y + rect.height * (1f - v0);
            float y1 = rect.y + rect.height * (1f - v1);

            float xPos = rect.x + (float)i / pixelCount * rect.width;
            float xNext = rect.x + (float)(i + 1) / pixelCount * rect.width;

            DrawLine(xPos, y0, xNext, y1, 2f);
        }

        GUI.color = prev;
    }

    private void DrawLine(float x1, float y1, float x2, float y2, float thickness)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float length = Mathf.Sqrt(dx * dx + dy * dy);
        if (length < 0.01f) return;

        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

        Matrix4x4 saved = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, new Vector2(x1, y1));
        GUI.DrawTexture(new Rect(x1, y1 - thickness * 0.5f, length, thickness), Texture2D.whiteTexture);
        GUI.matrix = saved;
    }

    private void DrawVitalValue(Rect rect, string label, string value, string unit, Color color)
    {
        Color prev = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.4f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        GUI.color = color * 0.7f;
        GUI.Label(new Rect(rect.x + 6, rect.y + 4, rect.width, 18), label, labelStyle);

        GUI.color = color;
        GUI.Label(new Rect(rect.x + 6, rect.y + 20, rect.width - 10, 36), value, valueStyle);

        if (!string.IsNullOrEmpty(unit))
        {
            GUI.color = color * 0.6f;
            GUI.Label(new Rect(rect.x + 6, rect.y + rect.height - 20, rect.width, 18), unit, unitStyle);
        }

        GUI.color = prev;
    }

    private void BuildStyles()
    {
        bgTex = new Texture2D(2, 2);
        Color bgCol = new Color(0.02f, 0.04f, 0.06f, 0.96f);
        bgTex.SetPixels(new[] { bgCol, bgCol, bgCol, bgCol });
        bgTex.Apply();

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = bgTex },
            padding = new RectOffset(0, 0, 0, 0),
            border = new RectOffset(2, 2, 2, 2)
        };

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.4f, 1f, 0.5f) },
            alignment = TextAnchor.MiddleLeft
        };

        valueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            alignment = TextAnchor.MiddleLeft
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            normal = { textColor = new Color(0.6f, 0.7f, 0.6f) },
            alignment = TextAnchor.MiddleLeft
        };

        unitStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.5f, 0.6f, 0.5f) },
            alignment = TextAnchor.MiddleLeft
        };
    }
}