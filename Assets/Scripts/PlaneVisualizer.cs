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
        if (floorMat == null)
            floorMat = Resources.Load<Material>("ARPlane/FeatheredPlaneMaterial");

        if (wallMat == null)
            wallMat = floorMat; // fallback to same if needed

        if (floorMat == null) Debug.LogError("[PlaneVisualizer] FloorMaterial not found!");
        if (wallMat == null) Debug.LogError("[PlaneVisualizer] WallMaterial not found!");

        // Add CharacterPlacer component if needed
        if (enableCharacterPlacement)
        {
            _characterPlacer = gameObject.GetComponent<CharacterPlacer>();
            if (_characterPlacer == null)
                _characterPlacer = gameObject.AddComponent<CharacterPlacer>();

            // Set the character prefab if it's assigned here
            if (characterPrefab != null)
            {
                // Use reflection to set the private field since it's SerializeField
                var fieldInfo = typeof(CharacterPlacer).GetField("characterPrefab",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null)
                    fieldInfo.SetValue(_characterPlacer, characterPrefab);
            }
        }
    }

    void OnEnable()
    {
        _planeManager = FindFirstObjectByType<ARPlaneManager>();
        if (_planeManager == null)
        {
            Debug.LogError("[PlaneVisualizer] No ARPlaneManager in scene!");
            enabled = false;
            return;
        }

#pragma warning disable CS0618
        _planeManager.planesChanged += OnPlanesChanged;
#pragma warning restore CS0618
        Debug.Log("[PlaneVisualizer] Started listening for planes...");
    }

    void OnDisable()
    {
        if (_planeManager != null)
        {
#pragma warning disable CS0618
            _planeManager.planesChanged -= OnPlanesChanged;
#pragma warning restore CS0618
        }
    }

#pragma warning disable CS0618
    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        foreach (var plane in args.added)
        {
            Debug.Log($"[PlaneVisualizer] New plane {plane.trackableId} alignment {plane.alignment}");
            if (!plane.TryGetComponent<MeshFilter>(out _))
                plane.gameObject.AddComponent<MeshFilter>();

            var viz = plane.GetComponent<ARPlaneMeshVisualizer>();
            if (viz == null)
                viz = plane.gameObject.AddComponent<ARPlaneMeshVisualizer>();

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
                    mr.material = floorMat; // fallback material
                    break;
            }

            // ✅ Always enable the renderer and visualizer
            mr.enabled = true;
            viz.enabled = true;
        }
    }
#pragma warning restore CS0618
}
