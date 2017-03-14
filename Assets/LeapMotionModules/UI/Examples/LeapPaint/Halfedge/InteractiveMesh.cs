using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Halfedge {

  [RequireComponent(typeof(MeshFilter))]
  public class InteractiveMesh : MonoBehaviour, IMeshChangeSubscriber {

    public PrimitiveType startingMesh = PrimitiveType.Tetrahedron;

    private HalfedgeMesh _hMesh;
    private MeshFilter _filter;
    private Mesh _mesh;

    void Start() {
      _filter = GetComponent<MeshFilter>();
      _mesh = new Mesh();
      _mesh.name = startingMesh.ToString();
      _filter.mesh = _mesh;

      _hMesh = new HalfedgeMesh();
      _hMesh.Subscribe(this);
      HalfedgeMesh.AddPrimitive(_hMesh, startingMesh);
    }

    private static List<Vector3> s_vertPosCache = new List<Vector3>();
    private static List<int> s_vertIdxCache = new List<int>();

    private static HashSet<Face> s_facesVisitedCache = new HashSet<Face>();
    private static List<Vertex> s_curFaceVertsCache = new List<Vertex>();
    public void OnHalfedgeStructureAdded(Halfedge fullyConnectedHalfedgeStructure) {

      s_vertPosCache.Clear();
      s_vertIdxCache.Clear();
      s_facesVisitedCache.Clear();

      int startingMeshVertCount = _mesh.vertices.Length;

      // Iterate through all faces in the fully connected halfedge structure.
      Face curFace = fullyConnectedHalfedgeStructure.face;
      Halfedge curHalfedge = curFace.halfedge;
      int curIdx = 0;
      do {
        // Loop face for verts.
        s_curFaceVertsCache.Clear();
        curHalfedge = curFace.halfedge;
        do {
          s_curFaceVertsCache.Add(curHalfedge.vertex);
          curHalfedge = curHalfedge.next;
        } while (curHalfedge != curFace.halfedge);

        // Add verts to unity mesh, add triangle to unity mesh.
        foreach (var vert in s_curFaceVertsCache) {
          AddVert(vert);
        }
        AddTri(curIdx, curIdx+1, curIdx+2, startingMeshVertCount);
        curIdx += 3;

        // Mark face as visited, find another face and keep going.
        s_facesVisitedCache.Add(curFace);
        curFace = FindNewFace(curFace);
      } while (curFace != null);

      // Finally, upload mesh data.
      _mesh.SetVertices(s_vertPosCache);
      _mesh.SetTriangles(s_vertIdxCache, 0, true);
      _mesh.RecalculateNormals();
    }

    private void AddVert(Vertex v) {
      s_vertPosCache.Add(v.position);
    }

    private void AddTri(int a, int b, int c, int offset = 0) {
      s_vertIdxCache.Add(a + offset);
      s_vertIdxCache.Add(c + offset);
      s_vertIdxCache.Add(b + offset);
    }

    private Face FindNewFace(Face face) {
      Halfedge curHalfedge = face.halfedge;
      Face newFace = null;
      do {
        Face testFace = curHalfedge.opposite.face;
        if (!s_facesVisitedCache.Contains(testFace)) {
          return testFace;
        }
        curHalfedge = curHalfedge.next;
      } while (curHalfedge != face.halfedge);

      return newFace;
    }

  }

}