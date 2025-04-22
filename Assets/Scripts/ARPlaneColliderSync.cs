using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARPlane))]
[RequireComponent(typeof(ARPlaneMeshVisualizer))]
[RequireComponent(typeof(MeshCollider))]
public class ARPlaneColliderSync : MonoBehaviour
{
    private MeshCollider _meshCollider;
    private ARPlaneMeshVisualizer _meshVisualizer;

    void Awake()
    {
        _meshCollider = GetComponent<MeshCollider>();
        _meshVisualizer = GetComponent<ARPlaneMeshVisualizer>();
    }

    void Update()
    {
        if (_meshVisualizer != null && _meshVisualizer.mesh != null)
        {
            if (_meshCollider.sharedMesh != _meshVisualizer.mesh)
                _meshCollider.sharedMesh = _meshVisualizer.mesh;
        }
    }
}
