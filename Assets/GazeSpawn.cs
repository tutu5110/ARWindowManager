using UnityEngine;
using UnityEngine.InputSystem;

public class GazeSpawn : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference doublePinch;      // Reference to the DoublePinch action (drag from Controls asset)

    [Header("Spawn Settings")]
    public GameObject cubePrefab;                // Optional: custom cube prefab. If empty, Unity will create a primitive cube
    public float spawnDistance = 1.5f;           // Distance in front of the camera where the cube will spawn
    public bool stickToSurface = false;          // If true, spawn on the surface hit by a raycast instead of fixed distance
    public float rayMaxDistance = 5f;            // Maximum raycast distance when stickToSurface is enabled
    public bool faceCamera = true;               // If true, cube will face the player
    public float cooldown = 0.25f;               // Cooldown time (seconds) between spawns to prevent double triggers

    Camera cam;
    float lastTime;

    void Awake()
    {
        cam = Camera.main;
        if (cam == null) Debug.LogError("Main Camera not found. Please tag your camera as MainCamera.");
    }

    void OnEnable()
    {
        if (doublePinch != null && doublePinch.action != null)
        {
            // Subscribe to the input event
            doublePinch.action.performed += OnDoublePinch;
            doublePinch.action.Enable();
        }
        else
        {
            Debug.LogError("Please drag the DoublePinch action into the 'doublePinch' field in the Inspector.");
        }
    }

    void OnDisable()
    {
        if (doublePinch != null && doublePinch.action != null)
        {
            // Unsubscribe when disabled
            doublePinch.action.performed -= OnDoublePinch;
            doublePinch.action.Disable();
        }
    }

    void OnDoublePinch(InputAction.CallbackContext ctx)
    {
        // Prevents spawning too frequently
        if (Time.time - lastTime < cooldown) return;
        lastTime = Time.time;
        if (cam == null) return;

        // Default spawn position = in front of the camera
        Vector3 pos = cam.transform.position + cam.transform.forward * spawnDistance;

        // If stickToSurface is enabled, cast a ray to hit surfaces
        if (stickToSurface &&
            Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, rayMaxDistance))
        {
            pos = hit.point;
        }

        // Decide cube rotation (face the camera or identity)
        Quaternion rot = faceCamera
            ? Quaternion.LookRotation(-cam.transform.forward, Vector3.up)
            : Quaternion.identity;

        // Spawn cube prefab if provided, otherwise create a primitive cube
        GameObject go = cubePrefab != null
            ? Instantiate(cubePrefab, pos, rot)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        go.transform.SetPositionAndRotation(pos, rot);

        // Default size for primitive cube
        if (cubePrefab == null)
        {
            go.transform.localScale = Vector3.one * 0.15f; // ~15 cm cube
        }

        // Add Rigidbody so the cube has physics (optional)
        if (!go.TryGetComponent<Rigidbody>(out _))
        {
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false; // Disable gravity so it floats
        }
    }
}
