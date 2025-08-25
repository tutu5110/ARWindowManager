using System;
using UnityEngine;

public class FollowGaze : MonoBehaviour
{
    public float distance = 1.5f;      // how far in front of the camera
    public bool faceCamera = true;     // keep facing the viewer

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Main Camera not found. Please tag your camera as MainCamera.");
        }
    }

    void LateUpdate()
    {
        Debug.Log("Target pos: " + transform.position);
        if (cam == null) return;

        // position directly in front of the camera
        transform.position = cam.transform.position + cam.transform.forward * distance;


        // optional rotation so the object faces the viewer
        if (faceCamera)
        {
            transform.rotation = Quaternion.LookRotation(-cam.transform.forward, Vector3.up);
        }
    }
}
