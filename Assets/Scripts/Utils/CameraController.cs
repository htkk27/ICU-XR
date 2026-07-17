using UnityEngine;

// Ελέγχει την κάμερα του παίκτη για να κάνει βόλτες μέσα στη ΜΕΘ
public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 3f;
    public float zoomSpeed = 2f;

    [Header("Boundaries")]
    public float minX = -8f;
    public float maxX = 8f;
    public float minZ = -8f;
    public float maxZ = 8f;
    public float minY = 0.5f;
    public float maxY = 8f;

    [Header("Collision")]
    public bool useCollision = true;
    public LayerMask collisionLayers = ~0;
    public float collisionRadius = 0.25f;
    public float collisionHeightOffset = 0.9f;
    public float collisionPadding = 0.05f;

    [Header("Mouse Look")]
    public bool enableMouseLook = true;
    public bool lockCursorOnStart = true;
    public float mouseSensitivity = 2f;
    private float rotationX = 0f;
    private float rotationY = 0f;

    [Header("Presets")]
    public Vector3 overviewPosition;
    public Vector3 overviewRotation;
    public Vector3 patientPosition;
    public Vector3 patientRotation;

    private void Start()
    {
        // Αρχικοποιούμε την περιστροφή με βάση το πώς είναι στημένη η κάμερα ήδη
        rotationX = transform.eulerAngles.y;
        rotationY = transform.eulerAngles.x;

        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandlePresets();
    }

    // Χειρίζεται την κίνηση με τα κλασικά κουμπιά (W, A, S, D και τα βελάκια)
    private void HandleMovement()
    {
        if (GameController.Instance?.isPaused == true) return;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Κίνηση τύπου FPS: πάμε μπροστά ανάλογα με το που κοιτάμε (χωρίς να πετάμε προς τα πάνω)
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 flatRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 move = (flatRight * moveX + flatForward * moveZ);
        Vector3 delta = move * moveSpeed * Time.deltaTime;

        if (useCollision)
        {
            // Αν δεν περνάει διαγώνια, δοκιμάζουμε να γλιστρήσει στον έναν άξονα.
            if (CanMove(delta))
            {
                transform.position += delta;
            }
            else
            {
                Vector3 deltaX = new Vector3(delta.x, 0f, 0f);
                Vector3 deltaZ = new Vector3(0f, 0f, delta.z);

                if (CanMove(deltaX))
                {
                    transform.position += deltaX;
                }

                if (CanMove(deltaZ))
                {
                    transform.position += deltaZ;
                }
            }
        }
        else
        {
            transform.position += delta;
        }

        // Δεν τον αφήνουμε να βγει έξω από τα όρια του δωματίου
        Vector3 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, minX, maxX);
        clampedPos.y = Mathf.Clamp(clampedPos.y, minY, maxY);
        clampedPos.z = Mathf.Clamp(clampedPos.z, minZ, maxZ);
        transform.position = clampedPos;
    }

    private bool CanMove(Vector3 delta)
    {
        if (delta.sqrMagnitude < 0.000001f)
        {
            return true;
        }

        Vector3 direction = delta.normalized;
        float distance = delta.magnitude + collisionPadding;
        Vector3 origin = transform.position + Vector3.up * collisionHeightOffset;

        return !Physics.SphereCast(origin, collisionRadius, direction, out _, distance, collisionLayers);
    }

    // Κουνάει την κάμερα με βάση το ποντίκι
    private void HandleRotation()
    {
        if (GameController.Instance?.isPaused == true) return;

        if (enableMouseLook && Cursor.lockState == CursorLockMode.Locked)
        {
            rotationX += Input.GetAxis("Mouse X") * mouseSensitivity;
            rotationY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            rotationY = Mathf.Clamp(rotationY, -90f, 90f);

            transform.eulerAngles = new Vector3(rotationY, rotationX, 0f);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool shouldUnlock = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = shouldUnlock ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = shouldUnlock;
        }
    }

    // Έτοιμες κάμερες (πχ στο κρεβάτι ή γενικό πλάνο) που αλλάζουν με τα νούμερα 1, 2
    private void HandlePresets()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            MoveTo(overviewPosition, overviewRotation);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            MoveTo(patientPosition, patientRotation);
        }
    }

    // Πάει την κάμερα ομαλά σε ένα σημείο
    public void MoveTo(Vector3 position, Vector3 rotation, float duration = 0.5f)
    {
        StopAllCoroutines();
        StartCoroutine(SmoothMoveTo(position, rotation, duration));
    }

    private System.Collections.IEnumerator SmoothMoveTo(Vector3 targetPos, Vector3 targetRot, float duration)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.Euler(targetRot);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Lerp(startRot, endRot, t);

            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = endRot;

        // Ενημερώνουμε τις μεταβλητές
        rotationX = targetRot.y;
        rotationY = targetRot.x;
    }
}