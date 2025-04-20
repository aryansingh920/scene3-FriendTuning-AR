using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARTapToPlaceCharacter : MonoBehaviour
{
    [Tooltip("Folder under Resources where your character prefab lives")]
    [SerializeField] string resourcePath = "Characters/character";

    ARRaycastManager _raycastManager;
    GameObject _characterPrefab;

    static List<ARRaycastHit> _hits = new List<ARRaycastHit>();

    void Awake()
    {
        // grab the ARRaycastManager in your scene
        _raycastManager = Object.FindFirstObjectByType<ARRaycastManager>();
        if (_raycastManager == null)
            Debug.LogError("ARTapToPlaceCharacter: No ARRaycastManager in scene.");

        // load your prefab
        _characterPrefab = Resources.Load<GameObject>(resourcePath);
        if (_characterPrefab == null)
            Debug.LogError($"ARTapToPlaceCharacter: Failed to load prefab at Resources/{resourcePath}");
    }

    void Update()
    {
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began)
            return;

        // raycast against detected planes
        if (_raycastManager.Raycast(touch.position, _hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = _hits[0].pose;
            Debug.Log($"ARTapToPlaceCharacter: Placing character at {hitPose.position}");
            Instantiate(_characterPrefab, hitPose.position, hitPose.rotation);
        }
    }
}
