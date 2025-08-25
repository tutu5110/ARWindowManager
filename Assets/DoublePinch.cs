using UnityEngine;
using Unity.PolySpatial.InputDevices;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.InputSystem.LowLevel;

public class DoublePinchSpawnAtGaze : MonoBehaviour
{
    [SerializeField] private float doublePinchTime = 0.3f;
    private float lastPinchTime;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void Update()
    {
        if (Touch.activeTouches.Count == 0) return;

        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase != TouchPhase.Began) continue;

            if (Time.time - lastPinchTime <= doublePinchTime)
            {
                SpatialPointerState state = EnhancedSpatialPointerSupport.GetPointerState(touch);

                // only proceed if gaze is on something
                if (state.targetObject != null)
                {
                    // optional: ensure you are staring at a cube
                    // if you do not need this check, remove the if block
                    if (state.targetObject.GetComponent<BoxCollider>() != null)
                    {
                        SpawnCube(state.targetObject.transform.position);
                    }
                }

                lastPinchTime = 0f;
            }
            else
            {
                lastPinchTime = Time.time;
            }
        }
    }

    private void SpawnCube(Vector3 position)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = position;
        cube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        // add gravity
        Rigidbody rb = cube.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.mass = 1f;
    }
}
