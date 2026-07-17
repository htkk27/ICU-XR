using UnityEngine;
using UnityEditor;

// Προσθέτει CameraController στην Main Camera
public class AddCameraController : EditorWindow
{
    [MenuItem("ICU XR/Add Camera Controls")]
    public static void AddCameraControls()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        // Ελέγχει αν υπάρχει ήδη
        CameraController existing = mainCamera.GetComponent<CameraController>();
        if (existing != null)
        {
            Debug.LogWarning("CameraController already exists!");
            return;
        }

        // Προσθήκη των components
        CameraController controller = mainCamera.gameObject.AddComponent<CameraController>();
        
        // Ρύθμιση των παραμέτρων
        controller.moveSpeed = 3f;
        controller.rotationSpeed = 2f;
        controller.enableMouseLook = false; // Απενεργοποιημένο by default
        
        Debug.Log("✅ CameraController added to Main Camera!");
        Debug.Log("🎮 Controls: WASD to move, Mouse to rotate (if enabled)");
    }
}
