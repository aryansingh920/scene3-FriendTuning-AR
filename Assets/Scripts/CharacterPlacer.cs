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

    void Start()
    {
        raycastManager = FindFirstObjectByType<ARRaycastManager>();
        planeManager = FindFirstObjectByType<ARPlaneManager>();

        if (raycastManager == null)
        {
            Debug.LogError("[CharacterPlacer] No ARRaycastManager found in scene!");
            enabled = false;
        }

        if (planeManager == null)
        {
            Debug.LogError("[CharacterPlacer] No ARPlaneManager found in scene!");
            enabled = false;
        }

        if (characterPrefab == null)
        {
            Debug.LogError("[CharacterPlacer] Character prefab not assigned!");
            enabled = false;
        }
    }

    void Update()
    {
        // Check for screen touches
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Only process touch began phase
            if (touch.phase == TouchPhase.Began)
            {
                // Raycast against planes
                if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
                {
                    // Get the first hit
                    ARRaycastHit hit = hits[0];

                    // Check if the hit plane is enabled/active
                    ARPlane plane = null;
                    foreach (var p in planeManager.trackables)
                    {
                        if (p.trackableId == hit.trackableId)
                        {
                            plane = p;
                            break;
                        }
                    }

                    // Only place character if plane is valid
                    if (plane != null && plane.gameObject.activeSelf)
                    {
                        // Create position from hit pose
                        Vector3 positionInWorld = hit.pose.position;

                        // Instantiate character at the hit position
                        // For horizontal planes, just use the hit position
                        // For vertical planes, adjust position to make character "stand" against the wall
                        if (plane.alignment == PlaneAlignment.Vertical)
                        {
                            // For vertical planes, make the character face the plane normal
                            Quaternion rotation = Quaternion.LookRotation(-hit.pose.up, Vector3.up);
                            Instantiate(characterPrefab, positionInWorld, rotation);
                            Debug.Log($"[CharacterPlacer] Placed character on vertical plane {plane.trackableId}");
                        }
                        else if (plane.alignment == PlaneAlignment.HorizontalUp)
                        {
                            // For floor planes, use original rotation 
                            // (assuming character's forward is already correctly aligned in the prefab)
                            Quaternion rotation = Quaternion.Euler(0, hit.pose.rotation.eulerAngles.y, 0);
                            Instantiate(characterPrefab, positionInWorld, rotation);
                            Debug.Log($"[CharacterPlacer] Placed character on floor plane {plane.trackableId}");
                        }
                        else
                        {
                            // Default case
                            Instantiate(characterPrefab, positionInWorld, hit.pose.rotation);
                            Debug.Log($"[CharacterPlacer] Placed character on other plane {plane.trackableId}");
                        }
                    }
                }
            }
        }
    }
}
