using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

[RequireComponent(typeof(ARRaycastManager))]
public class CharacterPlacer : MonoBehaviour
{
    [SerializeField] private GameObject characterPrefab;

    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject placedCharacter;
    private Camera mainCamera;

    void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager = FindFirstObjectByType<ARPlaneManager>();
        mainCamera = Camera.main;

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
            if (plane == null || !plane.gameObject.activeSelf) return;

            // adjust orientation if needed
            if (plane.alignment == PlaneAlignment.HorizontalUp)
                pose.position += Vector3.up * 0.05f; // small lift for realism

            if (placedCharacter == null)
            {
                placedCharacter = Instantiate(characterPrefab, pose.position, Quaternion.identity);
                placedCharacter.name = "ARCharacter";
                placedCharacter.transform.localScale = Vector3.one * 0.5f;
            }
            else
            {
                placedCharacter.transform.position = pose.position;
            }

            // make it always face the camera
            Vector3 directionToCamera = mainCamera.transform.position - placedCharacter.transform.position;
            directionToCamera.y = 0; // keep it on same horizontal plane
            placedCharacter.transform.rotation = Quaternion.LookRotation(directionToCamera.normalized);

            Debug.Log($"[CharacterPlacer] Placed/moved ARCharacter at {pose.position}.");
        }
    }
}
