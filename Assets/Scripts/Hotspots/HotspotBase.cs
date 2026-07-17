using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Το parent class που κληρονομούν όλα τα hotspots
public class HotspotBase : MonoBehaviour
{
    [Header("Hotspot Configuration")]
    public string hotspotId;
    public string hotspotLabel;
    public bool isActive = true;

    [Header("Visual Feedback")]
    public GameObject highlightObject;
    public Material normalMaterial;
    public Material highlightMaterial;
    public Material disabledMaterial;
    
    private Renderer[] renderers;
    private bool isHighlighted = false;

    // Events για να τα βλέπουμε και να τα σετάρουμε εύκολα από τον Inspector
    public UnityEvent onHotspotClicked;
    public UnityEvent onHotspotHover;

    protected virtual void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        UpdateVisualState();
        
        // Μπαίνουμε στη λίστα του manager
        HotspotManager.Instance?.RegisterHotspot(this);
    }

    protected virtual void OnDestroy()
    {
        // Βγαίνουμε από τη λίστα όταν καταστραφεί το object
        HotspotManager.Instance?.UnregisterHotspot(this);
    }

    // Τρέχει όταν ο παίκτης πατήσει πάνω στο hotspot
    public virtual void OnHotspotClick()
    {
        if (!isActive)
        {
            Debug.Log($"Hotspot {hotspotId} is not active");
            return;
        }

        Debug.Log($"Hotspot clicked: {hotspotId}");
        onHotspotClicked?.Invoke();
        
        // Κρατάμε log της αλληλεπίδρασης
        LoggingManager.Instance?.LogEvent("HOTSPOT_INTERACTION", new Dictionary<string, object>
        {
            { "hotspot_id", hotspotId },
            { "hotspot_label", hotspotLabel }
        });

        // Ενημερώνουμε το UI
        UIManager.Instance?.OnHotspotInteraction(this);
    }

    // Όταν το ποντίκι πάει πάνω στο hotspot
    public virtual void OnHotspotHoverEnter()
    {
        if (!isActive) return;
        
        SetHighlight(true);
        onHotspotHover?.Invoke();
        UIManager.Instance?.ShowTooltip(hotspotLabel);
    }

    // Όταν το ποντίκι φεύγει από το hotspot
    public virtual void OnHotspotHoverExit()
    {
        SetHighlight(false);
        UIManager.Instance?.HideTooltip();
    }

    // Ανοιγοκλείνει τη λειτουργικότητα του hotspot
    public void SetActive(bool active)
    {
        isActive = active;
        UpdateVisualState();
    }

    // Φτιάχνει τα materials ανάλογα με το αν είναι active/disabled κλπ.
    private void UpdateVisualState()
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(isActive && isHighlighted);
        }

        Material targetMaterial = isActive ? normalMaterial : disabledMaterial;
        if (targetMaterial != null && renderers != null)
        {
            foreach (var r in renderers)
            {
                r.material = targetMaterial;
            }
        }
    }

    // Χειρίζεται το highlight effect
    private void SetHighlight(bool highlight)
    {
        isHighlighted = highlight;
        
        if (highlightObject != null)
        {
            highlightObject.SetActive(isActive && isHighlighted);
        }

        if (highlight && highlightMaterial != null && renderers != null)
        {
            foreach (var r in renderers)
            {
                r.material = highlightMaterial;
            }
        }
        else if (!highlight && normalMaterial != null && renderers != null)
        {
            foreach (var r in renderers)
            {
                r.material = isActive ? normalMaterial : disabledMaterial;
            }
        }
    }

    // Mouse events για PC
    private void OnMouseEnter()
    {
        OnHotspotHoverEnter();
    }

    private void OnMouseExit()
    {
        OnHotspotHoverExit();
    }

    private void OnMouseDown()
    {
        OnHotspotClick();
    }
}