using Leap.Paint;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.StrokeProcessing.Rendering {

  [RequireComponent(typeof(MeshFilter))]
  [RequireComponent(typeof(MeshRenderer))]
  public class SegmentRenderer : MonoBehaviour {

    [Header("Segment Definitions")]
    [Tooltip("Must begin with a cyclical loop of the same length as the cross section mesh.")]
    public Mesh startCapMesh;
    [Tooltip("Must contain a cyclical loop of vertices, with no tris.")]
    public Mesh crossSectionMesh;
    [Tooltip("Must begin with a cyclical loop of the same length as the cross section mesh.")]
    public Mesh endCapMesh;

    private List<SegmentNode> _segmentNodes;
    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private bool _needsInit = true;

    private Vector3[] _startCapVerts;
    private Vector3[] _crossSectionVerts;
    private Vector3[] _endCapVerts;
    private List<Vector3> _meshVerts;
    private List<int> _meshIndices;

    void Start() {
      if (_needsInit) Initialize();
    }

    /// <summary>
    /// Initializes, or resets if already initialized.
    /// </summary>
    public void Initialize() {
      if (_segmentNodes == null) _segmentNodes = new List<SegmentNode>();
      else _segmentNodes.Clear();
      if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
      if (_mesh == null) _mesh = _meshFilter.mesh = new Mesh();
      else _mesh.Clear();
      if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
      _needsInit = false;

      if (_meshVerts == null) _meshVerts = new List<Vector3>();
      else _meshVerts.Clear();
      if (_meshIndices == null) _meshIndices = new List<int>();
      else _meshIndices.Clear();

      _startCapVerts = startCapMesh.vertices;
      _crossSectionVerts = crossSectionMesh.vertices;
      _endCapVerts = endCapMesh.vertices;
    }

    public void AddPoint(StrokePoint strokePoint) {
      SegmentNode node = new SegmentNode(strokePoint, this);
      _segmentNodes.Add(node);
      if (_segmentNodes.Count > 1) {
        node.prevNode = _segmentNodes[_segmentNodes.Count - 2];
        node.prevNode.nextNode = node;
        AddMeshSegment(node.prevNode, node);
      }
    }

    private List<SegmentNode> _cachedSegmentNodes = new List<SegmentNode>();
    public void ModifyPoints(List<int> modifiedPointsIndices) {
      _cachedSegmentNodes.Clear();
      for (int i = 0; i < modifiedPointsIndices.Count; i++) {
        _cachedSegmentNodes.Add(_segmentNodes[modifiedPointsIndices[i]]);
      }
      RefreshMeshVerts(_cachedSegmentNodes);
    }

    public void AddStartCap(int strokePointIndex) {
      SegmentNode node = _segmentNodes[strokePointIndex];
      node.hasStartCap = true;
      AddMeshStartCap(node);
    }

    public void AddEndCap(int strokePointIndex) {
      SegmentNode node = _segmentNodes[strokePointIndex];
      node.hasEndCap = true;
      AddMeshEndCap(node);
    }

    public void RemoveEndCapAtEnd() {
      SegmentNode node = _segmentNodes[_segmentNodes.Count - 1];
      node.hasEndCap = false;
      RemoveMeshEndCap(node);
    }

    /// <summary>
    /// SegmentNodes reference StrokePoints, but don't automatically listen to changes to those StrokePoints.
    /// So they always have access to any external StrokePoint modifications; the SegmentRender
    /// merely needs to be told when to re-evaluate StrokePoint data for SegmentNodes and refresh the
    /// Mesh data accordingly.
    /// </summary>
    private class SegmentNode {
      public SegmentRenderer renderer;
      public StrokePoint strokePoint;
      public SegmentNode(StrokePoint point, SegmentRenderer renderer) {
        this.renderer = renderer;
        this.strokePoint = point;
      }
      public bool hasStartCap = false;
      public bool hasEndCap = false;

      public SegmentNode prevNode;
      public SegmentNode nextNode;

      public bool hasCrossSectionVerts;
      public int crossSectionVertsIdx;

      public int CrossSectionVertsCount {
        get {
          return renderer._crossSectionVerts.Length;
        }
      }

      public int TotalVertsCount {
        get {
          int count = CrossSectionVertsCount;
          if (hasStartCap) count += renderer._startCapVerts.Length;
          if (hasEndCap) count += renderer._endCapVerts.Length;
          return count;
        }
      }

      /// <summary>Returns the vertex index around the cross section vertex ring (automatically wraps around based on cross-section vert count).</summary>
      public int GetCrossSectionVertIdx(int wrappingIndex) {
        int wrappedIndex = wrappingIndex % renderer._crossSectionVerts.Length;
        return crossSectionVertsIdx + wrappedIndex;
      }

      /// <summary> Returns the local-space (mesh) position of the vertex of this segment node at the given index along the cross-section vertex ring.</summary>
      public Vector3 GetVertAt(int crossSectionRingIndex) {
        return (this.strokePoint.rotation
                * Vector3.Scale(renderer._crossSectionVerts[crossSectionRingIndex % renderer._crossSectionVerts.Length],
                                 this.strokePoint.scale))
               + this.strokePoint.position;
      }
    }

    private void AddMeshSegment(SegmentNode node1, SegmentNode node2) {
      if (!node1.hasCrossSectionVerts) {
        AddCrossSectionVerts(node1);
      }
      if (!node2.hasCrossSectionVerts) {
        AddCrossSectionVerts(node2);
      }

      ConnectCrossSectionVerts(node1, node2);

      UploadMeshData();
    }

    private void AddCrossSectionVerts(SegmentNode node) {
      node.hasCrossSectionVerts = true;
      node.crossSectionVertsIdx = _meshVerts.Count;
      for (int i = 0; i < _crossSectionVerts.Length; i++) {
        _meshVerts.Add(node.GetVertAt(i));
      }
    }

    private void RefreshMeshVerts(List<SegmentNode> nodes) {
      for (int i = 0; i < nodes.Count; i++) {
        RefreshMeshVerts(nodes[i], uploadImmediately: false);
      }
      UploadMeshData();
    }

    private void RefreshMeshVerts(SegmentNode node, bool uploadImmediately = true) {
      for (int i = 0; i < node.CrossSectionVertsCount; i++) {
        _meshVerts[node.crossSectionVertsIdx + i] = node.GetVertAt(i);
      }
      if (uploadImmediately) {
        UploadMeshData();
      }
    }

    private void ConnectCrossSectionVerts(SegmentNode nodeA, SegmentNode nodeB) {
      for (int i = 0; i < _crossSectionVerts.Length; i++) {
        int v_a0 = nodeA.GetCrossSectionVertIdx(i + 0), v_a1 = nodeA.GetCrossSectionVertIdx(i + 1);
        int v_b0 = nodeB.GetCrossSectionVertIdx(i + 0), v_b1 = nodeB.GetCrossSectionVertIdx(i + 1);

        AddTri(v_a0, v_a1, v_b0);
        AddTri(v_a1, v_b1, v_b0);
      }
    }

    private void AddTri(int v0, int v1, int v2) {
      _meshIndices.Add(v0);
      _meshIndices.Add(v1);
      _meshIndices.Add(v2);
    }

    private void AddMeshStartCap(SegmentNode node) {
      //throw new System.NotImplementedException();
    }

    private void AddMeshEndCap(SegmentNode node) {
      //throw new System.NotImplementedException();
    }

    private void RemoveMeshEndCap(SegmentNode node) {
      //throw new System.NotImplementedException();
    }

    private void UploadMeshData() {
      _mesh.SetVertices(_meshVerts);
      _mesh.SetTriangles(_meshIndices, 0, true);
      _mesh.RecalculateNormals();
      _mesh.UploadMeshData(false);
    }

  }

}