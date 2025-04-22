using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections;

public class PlaneVisualizer : MonoBehaviour
{
    [SerializeField]
    private Material floorMat, wallMat;

    [SerializeField]
    private GameObject characterPrefab;

    [SerializeField]
    private bool enableCharacterPlacement = true;

    private ARPlaneManager _planeManager;

    private CubePlacer _cubePlacer;

    void Awake()
    {
        Debug.Log("[PlaneVisualizer] Awake: Starting initialization...");

        if (floorMat == null)
        {
            Debug.Log("[PlaneVisualizer] Awake: floorMat is null, attempting to load from Resources...");
            floorMat = Resources.Load<Material>("ARPlane/FeatheredPlaneMaterial");
            if (floorMat == null)
                Debug.LogError("[PlaneVisualizer] Awake: Failed to load floorMat from Resources/ARPlane/FeatheredPlaneMaterial!");
            else
                Debug.Log("[PlaneVisualizer] Awake: Successfully loaded floorMat from Resources.");
        }

        if (wallMat == null)
        {
            // Debug.Log("[PlaneVisualizer] Awake: wallMat is null, using floorMat as fallback...");
            wallMat = floorMat;
            if (wallMat == null)
                Debug.LogError("[PlaneVisualizer] Awake: wallMat fallback failed, floorMat is also null!");
            else
                Debug.Log("[PlaneVisualizer] Awake: wallMat set to floorMat as fallback.");
        }

        if (floorMat == null) Debug.LogError("[PlaneVisualizer] Awake: FloorMaterial not found!");
        if (wallMat == null) Debug.LogError("[PlaneVisualizer] Awake: WallMaterial not found!");

        // Add CubePlacer component if needed
        if (enableCharacterPlacement)
        {
            // Debug.Log("[PlaneVisualizer] Awake: enableCharacterPlacement is true, setting up CubePlacer...");
            _cubePlacer = gameObject.GetComponent<CubePlacer>();
            if (_cubePlacer == null)
            {
                // Debug.Log("[PlaneVisualizer] Awake: CubePlacer component not found, adding it...");
                _cubePlacer = gameObject.AddComponent<CubePlacer>();
                // Debug.Log("[PlaneVisualizer] Awake: CubePlacer component added.");
            }
            else
            {
                // Debug.Log("[PlaneVisualizer] Awake: CubePlacer component already exists.");
            }

            // Set the character prefab if it's assigned here
            if (characterPrefab != null)
            {
                // Debug.Log("[PlaneVisualizer] Awake: characterPrefab is assigned, setting it on CubePlacer...");
                var fieldInfo = typeof(CubePlacer).GetField("characterPrefab",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(_cubePlacer, characterPrefab);
                    // Debug.Log("[PlaneVisualizer] Awake: Successfully set characterPrefab on CubePlacer.");
                }
                else
                {
                    // Debug.LogError("[PlaneVisualizer] Awake: Failed to set characterPrefab on CubePlacer via reflection!");
                }
            }
            else
            {
                // Debug.LogWarning("[PlaneVisualizer] Awake: characterPrefab is null, CubePlacer will need to have it assigned in Inspector!");
            }
        }
        else
        {
            // Debug.Log("[PlaneVisualizer] Awake: enableCharacterPlacement is false, skipping CubePlacer setup.");
        }
    }

    void OnEnable()
    {
        // Debug.Log("[PlaneVisualizer] OnEnable: Enabling plane visualization...");
        _planeManager = FindFirstObjectByType<ARPlaneManager>();
        if (_planeManager == null)
        {
            // Debug.LogError("[PlaneVisualizer] OnEnable: No ARPlaneManager in scene! Disabling script.");
            enabled = false;
            return;
        }
        else
        {
            // Debug.Log("[PlaneVisualizer] OnEnable: ARPlaneManager found.");
        }

#pragma warning disable CS0618
        _planeManager.planesChanged += OnPlanesChanged;
#pragma warning restore CS0618
        // Debug.Log("[PlaneVisualizer] OnEnable: Started listening for planes...");
    }

    void OnDisable()
    {
        // Debug.Log("[PlaneVisualizer] OnDisable: Disabling plane visualization...");
        if (_planeManager != null)
        {
#pragma warning disable CS0618
            _planeManager.planesChanged -= OnPlanesChanged;
#pragma warning restore CS0618
            // Debug.Log("[PlaneVisualizer] OnDisable: Stopped listening for planes.");
        }
        else
        {
            // Debug.Log("[PlaneVisualizer] OnDisable: ARPlaneManager is null, no need to unsubscribe.");
        }
    }

#pragma warning disable CS0618
    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        foreach (var plane in args.added)
        {
            if (!plane.TryGetComponent<MeshFilter>(out _))
                plane.gameObject.AddComponent<MeshFilter>();

            var viz = plane.GetComponent<ARPlaneMeshVisualizer>();
            if (viz == null)
                viz = plane.gameObject.AddComponent<ARPlaneMeshVisualizer>();

            var mr = plane.GetComponent<MeshRenderer>();
            if (mr == null)
                mr = plane.gameObject.AddComponent<MeshRenderer>();

            switch (plane.alignment)
            {
                case PlaneAlignment.HorizontalUp:
                    mr.material = floorMat;
                    break;
                case PlaneAlignment.Vertical:
                    mr.material = wallMat;
                    break;
                default:
                    mr.material = floorMat;
                    break;
            }

            mr.enabled = true;
            viz.enabled = true;

            StartCoroutine(AssignColliderMeshAfterDelay(plane, viz));
        }
    }

    IEnumerator AssignColliderMeshAfterDelay(ARPlane plane, ARPlaneMeshVisualizer viz)
    {
        yield return new WaitForSeconds(0.5f); // let ARKit update mesh

        var collider = plane.GetComponent<MeshCollider>();
        if (collider == null)
            collider = plane.gameObject.AddComponent<MeshCollider>();

        if (viz.mesh != null)
        {
            collider.sharedMesh = viz.mesh;
            Debug.Log($"[PlaneVisualizer] Collider assigned to plane {plane.trackableId}");
        }
        else
        {
            Debug.LogWarning($"[PlaneVisualizer] Mesh still null after delay for plane {plane.trackableId}");
        }
    }



#pragma warning restore CS0618
}
