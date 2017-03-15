using Leap.Unity.Query;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Halfedge {

  [RequireComponent(typeof(MeshFilter))]
  public class InteractiveMesh : MonoBehaviour {

    public PrimitiveType startingMeshType = PrimitiveType.Tetrahedron;

    public GameObject interactiveVertexPrefab;
    public GameObject interactiveFacePrefab;

    private Halfedge _halfedgeMesh;
    private MeshFilter _filter;
    private Mesh _mesh;

    private List<InteractiveVertex> _interactiveVertices = new List<InteractiveVertex>();
    private Dictionary<Face, InteractiveFace> _interactiveFaceMapping = new Dictionary<Face, InteractiveFace>();

    void Start() {
      _filter = GetComponent<MeshFilter>();
      _mesh = new Mesh();
      _mesh.name = startingMeshType.ToString();
      _filter.mesh = _mesh;

      _halfedgeMesh = Primitives.CreatePrimitive(startingMeshType);

      // Loop common vertices, construct InteractiveVertex objects
      _interactiveVertices.Clear();
      Ops.TraverseCommonVertices(_halfedgeMesh, (verts) => {
        _interactiveVertices.Add(InteractiveVertex.Create(this, verts, interactiveVertexPrefab));
      });

      // Loop faces, construct InteractiveFace objects
      _interactiveFaceMapping.Clear();
      foreach (var face in _halfedgeMesh.faces) {
        _interactiveFaceMapping[face] = InteractiveFace.Create(this, face, interactiveFacePrefab);
      }

      // Construct Unity mesh data by traversing faces and upload.
      RebuildUnityMeshData();
    }

    [ThreadStatic]
    private static List<InteractiveVertex> s_intVertCache = new List<InteractiveVertex>();
    public List<InteractiveVertex> GetInteractiveVertices(Face face) {
      s_intVertCache.Clear();
      foreach (var intVert in _interactiveVertices.Query()
                                .Where((qIntVert) => {
                                  return qIntVert.commonVertices.Query()
                                    .Any((vert) => { return vert.halfedge.face == face; });
                                })) {
        s_intVertCache.Add(intVert);
      }
      return s_intVertCache;
    }

    [ThreadStatic]
    private static List<Face> s_faceCache = new List<Face>();
    public List<Face> GetNeighboringFaces(Face face) {
      s_faceCache.Clear();
      foreach (var halfedge in face.halfedges) {
        s_faceCache.Add(halfedge.opposite.face);
      }
      return s_faceCache;
    }

    public InteractiveFace GetInteractiveFace(Face face) {
      return _interactiveFaceMapping[face];
    }

    private static List<Vector3> s_vertPosCache = new List<Vector3>();
    private static List<int> s_vertIdxCache = new List<int>();
    public void RebuildUnityMeshData() {
      _mesh.Clear();
      s_vertPosCache.Clear();
      s_vertIdxCache.Clear();

      GetUnityMeshData(_halfedgeMesh, s_vertPosCache, s_vertIdxCache);

      _mesh.SetVertices(s_vertPosCache);
      _mesh.SetTriangles(s_vertIdxCache, 0, true);
      _mesh.RecalculateNormals();
    }

    private static List<Vertex> s_curFaceVertsCache = new List<Vertex>();
    private void GetUnityMeshData(Halfedge halfedgeStructure, List<Vector3> outVerts, List<int> outIndices) {
      int v0 = outVerts.Count;
      outVerts.Clear();
      outIndices.Clear();

      // Iterate through all faces in the fully connected halfedge structure.
      Halfedge h, h0;
      int curVertIdx = 0;
      foreach (var face in halfedgeStructure.faces) {
        // Loop face for verts.
        s_curFaceVertsCache.Clear();
        h = h0 = face.halfedge;
        do {
          s_curFaceVertsCache.Add(h.vertex);
          h = h.next;
        } while (h != h0);

        // Add verts to unity mesh, add triangle to unity mesh.
        foreach (var vert in s_curFaceVertsCache) {
          outVerts.AddVert(vert);
        }
        outIndices.AddTri(curVertIdx, curVertIdx + 1, curVertIdx + 2, v0);
        curVertIdx += 3;
      }
    }
  }

  public static class UnityMeshExtensions {

    public static void AddVert(this List<Vector3> verts, Vertex v) {
      verts.Add(v.position);
    }

    public static void AddTri(this List<int> idxs, int a, int b, int c, int offset = 0) {
      idxs.Add(a + offset);
      idxs.Add(c + offset);
      idxs.Add(b + offset);
    }

  }

}