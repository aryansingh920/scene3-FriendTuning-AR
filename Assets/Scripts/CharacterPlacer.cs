using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class CharacterPlacer : MonoBehaviour
{
    [SerializeField]
    private GameObject characterPrefab;

    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private Material debugMaterial;

    void Start()
    {
        Debug.Log("[CharacterPlacer] Start: Starting initialization...");

        raycastManager = FindFirstObjectByType<ARRaycastManager>();
        if (raycastManager == null)
        {
            Debug.LogError("[CharacterPlacer] Start: No ARRaycastManager found! Disabling.");
            enabled = false;
            return;
        }

        planeManager = FindFirstObjectByType<ARPlaneManager>();
        if (planeManager == null)
        {
            Debug.LogError("[CharacterPlacer] Start: No ARPlaneManager found! Disabling.");
            enabled = false;
            return;
        }

        if (characterPrefab == null)
        {
            Debug.LogError("[CharacterPlacer] Start: Character prefab not assigned! Disabling.");
            enabled = false;
            return;
        }

        // Load material from Resources
        debugMaterial = Resources.Load<Material>("Materials/DebugMaterial");
        if (debugMaterial == null)
        {
            Debug.LogError("[CharacterPlacer] Start: Could not load DebugMaterial from Resources/Materials!");
        }
        else
        {
            Debug.Log("[CharacterPlacer] Start: Successfully loaded DebugMaterial.");
        }

        // Optionally reset AR session
        var arSession = FindFirstObjectByType<ARSession>();
        if (arSession != null)
        {
            Debug.Log("[CharacterPlacer] Start: Resetting ARSession...");
            arSession.Reset();
        }
        else
        {
            Debug.LogWarning("[CharacterPlacer] Start: ARSession not found.");
        }

        Debug.Log("[CharacterPlacer] Start: Initialization complete.");
    }

    void Update()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            ARRaycastHit hit = hits[0];
            Pose hitPose = hit.pose;

            ARPlane plane = planeManager.GetPlane(hit.trackableId);
            if (plane == null || !plane.gameObject.activeSelf)
            {
                Debug.LogWarning("[CharacterPlacer] Update: Inactive or missing plane.");
                return;
            }

            Quaternion rotation = Quaternion.identity;
            Vector3 position = hitPose.position;

            if (plane.alignment == PlaneAlignment.Vertical)
            {
                rotation = Quaternion.LookRotation(-plane.transform.forward, Vector3.up);
            }
            else if (plane.alignment == PlaneAlignment.HorizontalUp)
            {
                position.y += 0.1f;
            }
            else
            {
                rotation = hitPose.rotation;
            }

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = position;
            cube.transform.rotation = rotation;
            cube.transform.localScale = Vector3.one * 0.1f;
            cube.name = "TestCube";

            if (debugMaterial != null)
            {
                cube.GetComponent<Renderer>().material = debugMaterial;
            }
            else
            {
                Debug.LogWarning("[CharacterPlacer] Update: DebugMaterial not loaded, cube will use default.");
            }

            cube.AddComponent<BoxCollider>(); // this works if manually added like this

            // Add physics
            Rigidbody rb = cube.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.useGravity = true; // or false depending on your use case
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;


            Debug.Log($"[CharacterPlacer] Update: Spawned TestCube at {position}.");
        }
    }
}
