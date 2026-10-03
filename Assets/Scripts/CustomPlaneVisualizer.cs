using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Attach to the AR Plane prefab. Replaces the default visualizer mesh with a
/// custom quad that shows the student name texture. Hides itself when no plane
/// is tracked.
/// </summary>
[RequireComponent(typeof(ARPlane))]
public class CustomPlaneVisualizer : MonoBehaviour
{
    [Tooltip("Material whose albedo texture contains the student name.")]
    public Material planeMaterial;

    MeshFilter _meshFilter;
    MeshRenderer _meshRenderer;
    ARPlane _plane;

    void Awake()
    {
        _plane = GetComponent<ARPlane>();
        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        if (planeMaterial != null) _meshRenderer.material = planeMaterial;
    }

    void OnEnable() => _plane.boundaryChanged += OnBoundaryChanged;
    void OnDisable() => _plane.boundaryChanged -= OnBoundaryChanged;

    void OnBoundaryChanged(ARPlaneBoundaryChangedEventArgs args)
    {
        bool tracked = _plane.trackingState == TrackingState.Tracking;
        _meshRenderer.enabled = tracked;
        if (tracked) RebuildMesh();
    }

    void RebuildMesh()
    {
        var boundary = _plane.boundary;
        if (boundary.Length < 3) return;

        var verts = new Vector3[boundary.Length];
        for (int i = 0; i < boundary.Length; i++)
            verts[i] = new Vector3(boundary[i].x, 0f, boundary[i].y);

        // Simple fan triangulation
        var tris = new int[(boundary.Length - 2) * 3];
        for (int i = 0; i < boundary.Length - 2; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        var uvs = new Vector2[boundary.Length];
        for (int i = 0; i < boundary.Length; i++)
            uvs[i] = new Vector2(verts[i].x * 0.5f + 0.5f, verts[i].z * 0.5f + 0.5f);

        var mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        _meshFilter.mesh = mesh;
    }
}
