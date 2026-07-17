using System.Collections.Generic;
using UnityEngine;

// Αναλαμβάνει το raycasting από την κάμερα για να βρίσκει τα hotspots και τα UI effects τους
public class HotspotRaycaster : MonoBehaviour
{
    [Header("Interaction")]
    public float interactDistance = 5f;
    public KeyCode secondaryInteractKey = KeyCode.E;
    public LayerMask interactionLayers = ~0;

    [Header("Crosshair")]
    public bool showCrosshair = true;
    public float crosshairSize = 18f;
    public float crosshairThickness = 2f;
    public Color crosshairNormal = new Color(1f, 1f, 1f, 0.7f);
    public Color crosshairHover = new Color(0.3f, 1f, 0.4f, 1f);

    [Header("Hotspot Glow")]
    public Color glowColor = new Color(0.3f, 0.8f, 1f, 1f);
    public float glowIntensity = 0.4f;

    [Header("Floating Labels")]
    public bool showFloatingLabels = true;
    public float labelDistance = 8f;

    [Header("Alarm")]
    public bool showAlarmEffect = true;

    private Camera cachedCamera;
    private HotspotBase hoveredHotspot;
    private GUIStyle promptStyle;
    private GUIStyle promptShadowStyle;
    private GUIStyle labelStyle;
    private GUIStyle labelBgStyle;

    // Κρατάμε τα original χρώματα για να μπορούμε να επαναφέρουμε το glow 
    private Dictionary<Renderer, Color> originalEmission = new Dictionary<Renderer, Color>();
    private Dictionary<Renderer, bool> hadEmission = new Dictionary<Renderer, bool>();

    // Timers & εφέ
    private float clickFlashTimer = 0f;
    private float clickFlashDuration = 0.15f;
    private float alarmPulse = 0f;
    private Texture2D vignetteTexture;
    private float crosshairPulse = 0f;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();
        if (cachedCamera == null)
            cachedCamera = Camera.main;
        CreateVignetteTexture();
    }

    private void Update()
    {
        // Αν δεν έχουμε κλειδωμένο mouse, δεν κάνουμε τίποτα
        if (Cursor.lockState != CursorLockMode.Locked || cachedCamera == null)
        {
            ClearHover();
            return;
        }

        Ray ray = cachedCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        HotspotBase hit = null;
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, interactionLayers);
        foreach (var h in hits)
        {
            HotspotBase hs = h.collider.GetComponentInParent<HotspotBase>();
            if (hs != null)
            {
                hit = hs;
                break;
            }
        }

        // Αλλαγή στόχου - καθαρίζουμε το παλιό, σετάρουμε το νέο
        if (hit != hoveredHotspot)
        {
            RemoveGlow();

            if (hoveredHotspot != null)
                hoveredHotspot.OnHotspotHoverExit();

            hoveredHotspot = hit;

            if (hoveredHotspot != null && hoveredHotspot.isActive)
            {
                hoveredHotspot.OnHotspotHoverEnter();
                ApplyGlow(hoveredHotspot.gameObject);
            }
        }

        // Χειρισμός αλληλεπιδράσεων
        bool interactPressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(secondaryInteractKey);
        if (interactPressed && hoveredHotspot != null)
        {
            hoveredHotspot.OnHotspotClick();
            clickFlashTimer = clickFlashDuration;
            ICUAudioManager.Instance?.PlayClickSound();
        }

        // Ανανέωση timers
        if (clickFlashTimer > 0f)
            clickFlashTimer -= Time.deltaTime;

        crosshairPulse += Time.deltaTime * 3f;
        alarmPulse += Time.deltaTime * 4f;
    }

    // Glow System 

    // Ενεργοποιεί το emission στα materials για το highlight
    private void ApplyGlow(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            foreach (Material mat in r.materials)
            {
                originalEmission[r] = mat.GetColor("_EmissionColor");
                hadEmission[r] = mat.IsKeywordEnabled("_EMISSION");
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", glowColor * glowIntensity);
            }
        }
    }

    // Επαναφέρει τα materials στην αρχική τους κατάσταση
    private void RemoveGlow()
    {
        foreach (var kvp in originalEmission)
        {
            if (kvp.Key == null) continue;
            foreach (Material mat in kvp.Key.materials)
            {
                mat.SetColor("_EmissionColor", kvp.Value);
                if (hadEmission.ContainsKey(kvp.Key) && !hadEmission[kvp.Key])
                    mat.DisableKeyword("_EMISSION");
            }
        }
        originalEmission.Clear();
        hadEmission.Clear();
    }

    private void ClearHover()
    {
        RemoveGlow();
        if (hoveredHotspot != null)
        {
            hoveredHotspot.OnHotspotHoverExit();
            hoveredHotspot = null;
        }
    }

    private void OnGUI()
    {
        if (showAlarmEffect) DrawAlarmEffect();
        if (clickFlashTimer > 0f) DrawClickFlash();
        if (Cursor.lockState != CursorLockMode.Locked) return;

        if (showCrosshair) DrawCrosshair();
        if (hoveredHotspot != null && hoveredHotspot.isActive) DrawInteractionPrompt();
        if (showFloatingLabels) DrawFloatingLabels();
    }

    // Ζωγραφίζει τον στόχο (crosshair) στη μέση της οθόνης
    private void DrawCrosshair()
    {
        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;

        bool isHover = hoveredHotspot != null && hoveredHotspot.isActive;
        float pulse = isHover ? 1f + Mathf.Sin(crosshairPulse) * 0.15f : 1f;
        float size = crosshairSize * pulse;
        float half = size * 0.5f;
        float t = crosshairThickness;

        Color col = isHover ? crosshairHover : crosshairNormal;
        Texture2D tex = Texture2D.whiteTexture;
        
        Color prev = GUI.color;
        GUI.color = col;

        // Σταυρός
        GUI.DrawTexture(new Rect(cx - half, cy - t * 0.5f, half - 3f, t), tex);
        GUI.DrawTexture(new Rect(cx + 3f, cy - t * 0.5f, half - 3f, t), tex);
        GUI.DrawTexture(new Rect(cx - t * 0.5f, cy - half, t, half - 3f), tex);
        GUI.DrawTexture(new Rect(cx - t * 0.5f, cy + 3f, t, half - 3f), tex);

        if (isHover)
        {
            // Γωνίες όταν κάνουμε hover πάνω από αντικείμενο
            float corner = 4f;
            float offset = half + 2f;
            GUI.DrawTexture(new Rect(cx - offset, cy - offset, corner, t), tex);
            GUI.DrawTexture(new Rect(cx - offset, cy - offset, t, corner), tex);
            GUI.DrawTexture(new Rect(cx + offset - corner, cy - offset, corner, t), tex);
            GUI.DrawTexture(new Rect(cx + offset - t, cy - offset, t, corner), tex);
            GUI.DrawTexture(new Rect(cx - offset, cy + offset - t, corner, t), tex);
            GUI.DrawTexture(new Rect(cx - offset, cy + offset - corner, t, corner), tex);
            GUI.DrawTexture(new Rect(cx + offset - corner, cy + offset - t, corner, t), tex);
            GUI.DrawTexture(new Rect(cx + offset - t, cy + offset - corner, t, corner), tex);
        }
        else
        {
            // Κεντρική τελίτσα
            GUI.DrawTexture(new Rect(cx - 1f, cy - 1f, 2f, 2f), tex);
        }

        GUI.color = prev;
    }

    // Το μήνυμα "Πάτα Ε..." όταν κοιτάμε ένα hotspot
    private void DrawInteractionPrompt()
    {
        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            promptShadowStyle = new GUIStyle(promptStyle)
            {
                normal = { textColor = new Color(0, 0, 0, 0.85f) }
            };
        }

        string label = hoveredHotspot.hotspotLabel;
        if (string.IsNullOrEmpty(label)) label = hoveredHotspot.hotspotId;

        string text = $"[ {label} ]\n▶ Πάτα E ή Click";

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f + crosshairSize + 14f;
        Rect rect = new Rect(cx - 160f, cy, 320f, 50f);

        // Background
        Color prev = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.DrawTexture(new Rect(rect.x + 10, rect.y - 2, rect.width - 20, rect.height), Texture2D.whiteTexture);
        GUI.color = prev;

        // Κείμενο (με σκιά)
        Rect shadow = rect;
        shadow.x += 1f;
        shadow.y += 1f;
        GUI.Label(shadow, text, promptShadowStyle);
        GUI.Label(rect, text, promptStyle);
    }

    // Ταμπελάκια που αιωρούνται πάνω από τα hotspots
    private void DrawFloatingLabels()
    {
        if (cachedCamera == null) return;
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.95f, 1f, 0.9f) }
            };
        }

        HotspotBase[] hotspots = FindObjectsOfType<HotspotBase>();
        foreach (HotspotBase hs in hotspots)
        {
            if (!hs.isActive || hs == hoveredHotspot) continue;

            float dist = Vector3.Distance(cachedCamera.transform.position, hs.transform.position);
            if (dist > labelDistance) continue;

            Vector3 worldPos = hs.transform.position + Vector3.up * 0.5f;
            Vector3 screenPos = cachedCamera.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0) continue;

            // Fade-out αναλόγως απόστασης
            float alpha = Mathf.Clamp01(1f - (dist / labelDistance)) * 0.8f;
            Color prev = GUI.color;
            
            float sx = screenPos.x;
            float sy = Screen.height - screenPos.y;

            string name = !string.IsNullOrEmpty(hs.hotspotLabel) ? hs.hotspotLabel : hs.hotspotId;

            // Δείκτης σαν διαμαντάκι
            Texture2D tex = Texture2D.whiteTexture;
            GUI.color = new Color(0.3f, 0.8f, 1f, alpha * 0.7f);
            GUI.DrawTexture(new Rect(sx - 3f, sy - 3f, 6f, 6f), tex);

            GUI.color = new Color(1, 1, 1, alpha);
            GUI.Label(new Rect(sx - 80f, sy - 24f, 160f, 20f), name, labelStyle);

            GUI.color = prev;
        }
    }

    // Λευκό flash οθόνης όταν κάνουμε κλικ
    private void DrawClickFlash()
    {
        float alpha = (clickFlashTimer / clickFlashDuration) * 0.25f;
        Color prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prev;
    }

    // Κόκκινο εφέ οθόνης σε συναγερμό
    private void DrawAlarmEffect()
    {
        if (GameStateManager.Instance == null) return;
        if (GameStateManager.Instance.currentVitals.spo2 >= 90) return;

        float pulse = (Mathf.Sin(alarmPulse) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(0.05f, 0.2f, pulse);

        Color prev = GUI.color;
        GUI.color = new Color(1f, 0f, 0f, alpha);

        if (vignetteTexture != null)
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTexture);
        else
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        GUI.color = prev;
    }

    // Φτιάχνει τη vignette υφή για το alarm effect
    private void CreateVignetteTexture()
    {
        int size = 256;
        vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float maxDist = center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float t = Mathf.Clamp01(dist / maxDist);
                float alpha = t * t;
                vignetteTexture.SetPixel(x, y, new Color(1f, 0f, 0f, alpha));
            }
        }
        vignetteTexture.Apply();
    }

    private void OnDisable()
    {
        ClearHover();
    }
}