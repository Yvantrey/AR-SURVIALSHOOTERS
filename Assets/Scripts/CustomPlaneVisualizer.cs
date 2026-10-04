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
    Mesh _mesh;

    void Awake()
    {
        _plane = GetComponent<ARPlane>();
        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        _mesh = new Mesh { name = "AR Plane Name Mesh" };
        _meshFilter.sharedMesh = _mesh;
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

        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
        for (int i = 0; i < boundary.Length; i++)
        {
            minX = Mathf.Min(minX, boundary[i].x); maxX = Mathf.Max(maxX, boundary[i].x);
            minY = Mathf.Min(minY, boundary[i].y); maxY = Mathf.Max(maxY, boundary[i].y);
        }

        var uvs = new Vector2[boundary.Length];
        for (int i = 0; i < boundary.Length; i++)
            uvs[i] = new Vector2(Mathf.InverseLerp(minX, maxX, boundary[i].x), Mathf.InverseLerp(minY, maxY, boundary[i].y));

        _mesh.Clear();
        _mesh.vertices = verts;
        _mesh.triangles = tris;
        _mesh.uv = uvs;
        _mesh.RecalculateNormals();
    }
}
