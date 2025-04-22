using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaneVisualizer : MonoBehaviour
{
    [SerializeField]
    private Material floorMat, wallMat;

    [SerializeField]
    private GameObject characterPrefab;

    [SerializeField]
    private bool enableCharacterPlacement = true;

    private ARPlaneManager _planeManager;

    private CharacterPlacer _characterPlacer;

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

        // Add CharacterPlacer component if needed
        if (enableCharacterPlacement)
        {
            // Debug.Log("[PlaneVisualizer] Awake: enableCharacterPlacement is true, setting up CharacterPlacer...");
            _characterPlacer = gameObject.GetComponent<CharacterPlacer>();
            if (_characterPlacer == null)
            {
                // Debug.Log("[PlaneVisualizer] Awake: CharacterPlacer component not found, adding it...");
                _characterPlacer = gameObject.AddComponent<CharacterPlacer>();
                // Debug.Log("[PlaneVisualizer] Awake: CharacterPlacer component added.");
            }
            else
            {
                // Debug.Log("[PlaneVisualizer] Awake: CharacterPlacer component already exists.");
            }

            // Set the character prefab if it's assigned here
            if (characterPrefab != null)
            {
                // Debug.Log("[PlaneVisualizer] Awake: characterPrefab is assigned, setting it on CharacterPlacer...");
                var fieldInfo = typeof(CharacterPlacer).GetField("characterPrefab",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(_characterPlacer, characterPrefab);
                    // Debug.Log("[PlaneVisualizer] Awake: Successfully set characterPrefab on CharacterPlacer.");
                }
                else
                {
                    // Debug.LogError("[PlaneVisualizer] Awake: Failed to set characterPrefab on CharacterPlacer via reflection!");
                }
            }
            else
            {
                // Debug.LogWarning("[PlaneVisualizer] Awake: characterPrefab is null, CharacterPlacer will need to have it assigned in Inspector!");
            }
        }
        else
        {
            // Debug.Log("[PlaneVisualizer] Awake: enableCharacterPlacement is false, skipping CharacterPlacer setup.");
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
            // Ensure MeshFilter exists
            if (!plane.TryGetComponent<MeshFilter>(out _))
                plane.gameObject.AddComponent<MeshFilter>();

            // Ensure ARPlaneMeshVisualizer exists
            var viz = plane.GetComponent<ARPlaneMeshVisualizer>();
            if (viz == null)
                viz = plane.gameObject.AddComponent<ARPlaneMeshVisualizer>();

            // Ensure MeshRenderer exists
            var mr = plane.GetComponent<MeshRenderer>();
            if (mr == null)
                mr = plane.gameObject.AddComponent<MeshRenderer>();

            // Assign material based on alignment
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

            // Ensure MeshCollider exists
            var collider = plane.GetComponent<MeshCollider>();
            if (collider == null)
                collider = plane.gameObject.AddComponent<MeshCollider>();

            // Assign mesh to collider if available
            if (viz.mesh != null)
            {
                collider.sharedMesh = viz.mesh;
                collider.convex = false; // Convex false for static mesh collider
            }
            else
            {
                Debug.LogWarning($"[PlaneVisualizer] Plane '{plane.trackableId}' mesh not available yet — collider may not work until updated.");
            }
        }
    }


#pragma warning restore CS0618
}
