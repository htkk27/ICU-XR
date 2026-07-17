using UnityEngine;
using UnityEditor;

// Προσθέτει το HelpSystem στο scene
public class AddHelpSystem : EditorWindow
{
    [MenuItem("ICU XR/Setup Help System (F1)")]
    public static void SetupHelpSystem()
    {
        // Βρες ή δημιούργησε HelpSystem GameObject
        GameObject helpSystemGO = GameObject.Find("HelpSystem");
        if (helpSystemGO == null)
        {
            helpSystemGO = new GameObject("HelpSystem");
        }

        // Πρόσθεσε το component
        HelpSystem helpSystem = helpSystemGO.GetComponent<HelpSystem>();
        if (helpSystem == null)
        {
            helpSystem = helpSystemGO.AddComponent<HelpSystem>();
        }

        // Βρες το HelpPanel
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Transform helpPanel = canvas.transform.Find("HelpPanel");
            if (helpPanel != null)
            {
                helpSystem.helpPanel = helpPanel.gameObject;
                
                // Βεβαιώσου ότι το panel είναι κρυμμένο αρχικά
                helpPanel.gameObject.SetActive(false);
                
                Debug.Log("✅ HelpSystem setup complete!");
                Debug.Log("🎯 Press F1 in Play mode to toggle help");
            }
            else
            {
                Debug.LogWarning("⚠️ HelpPanel not found. Run 'Setup UI Panels' first!");
            }
        }
        else
        {
            Debug.LogError("❌ Canvas not found!");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );
    }
}
