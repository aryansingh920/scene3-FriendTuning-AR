using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

[RequireComponent(typeof(ARRaycastManager))]
public class CharacterPlacer : MonoBehaviour
{
    // If you’ve already wired a prefab in the Inspector you can skip the Resources.Load
    [SerializeField] private GameObject characterPrefab;

    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager = FindFirstObjectByType<ARPlaneManager>();

        // fallback to Resources if you didn’t assign in the Editor
        if (characterPrefab == null)
        {
            characterPrefab = Resources.Load<GameObject>("Characters/boy");
            if (characterPrefab == null)
            {
                Debug.LogError("[CharacterPlacer] Couldn’t load Characters/boy from Resources!");
                enabled = false;
                return;
            }
        }
    }

    void Update()
    {
        if (Input.touchCount == 0) return;
        var touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            var hit = hits[0];
            var pose = hit.pose;

            var plane = planeManager.GetPlane(hit.trackableId);
            if (plane == null || !plane.gameObject.activeSelf)
            {
                Debug.LogWarning("[CharacterPlacer] Hit inactive or missing plane.");
                return;
            }

            // adjust orientation & height if you need
            if (plane.alignment == PlaneAlignment.HorizontalUp)
                pose.position += Vector3.up * 0.1f;
            else if (plane.alignment == PlaneAlignment.Vertical)
                pose.rotation = Quaternion.LookRotation(-plane.transform.forward);

            // instantiate your character prefab
            var go = Instantiate(characterPrefab, pose.position, pose.rotation);
            go.name = "ARCharacter";

            Debug.Log($"[CharacterPlacer] Spawned {go.name} at {pose.position}.");
        }
    }
}
