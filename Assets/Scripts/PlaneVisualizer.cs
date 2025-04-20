using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaneVisualizer : MonoBehaviour
{
    ARPlaneManager _planeManager;
    Material _floorMat, _wallMat;

    void Awake()
    {
        _floorMat = Resources.Load<Material>("ARPlane/FeatheredPlaneMaterial");
        _wallMat = _floorMat; // fallback to same if needed

        if (_floorMat == null) Debug.LogError("[PlaneVisualizer] FloorMaterial not found!");
        if (_wallMat == null) Debug.LogError("[PlaneVisualizer] WallMaterial not found!");
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
#pragma warning disable CS0618
        _planeManager.planesChanged -= OnPlanesChanged;
#pragma warning restore CS0618
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
                    mr.material = _floorMat;
                    break;
                case PlaneAlignment.Vertical:
                    mr.material = _wallMat;
                    break;
                default:
                    mr.material = _floorMat; // fallback material
                    break;
            }

            // ✅ Always enable the renderer and visualizer
            mr.enabled = true;
            viz.enabled = true;
        }
    }
#pragma warning restore CS0618
}
