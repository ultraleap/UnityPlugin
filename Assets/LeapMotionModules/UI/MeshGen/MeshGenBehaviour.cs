using Leap.Unity.Attributes;
using Leap.Unity.UI;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine;
#endif

namespace Leap.Unity.UI {

  [RequireComponent(typeof(MeshFilter))]
  [ExecuteInEditMode]
  public abstract class MeshGenBehaviour : MonoBehaviour {

    public bool newMeshNow = false;
    private bool _newMeshNowState = false;

    private MeshFilter _filter;

    void OnValidate() {
      InitMesh(newMeshNow == true && newMeshNow != _newMeshNowState);
    }

    void Start() {
      InitMesh(true);
    }

    private static ulong _genCount = 0uL;
    List<Vector3> _vertCache = new List<Vector3>();
    List<int> _idxCache = new List<int>();
    List<Vector3> _normalCache = new List<Vector3>();
    private void InitMesh(bool forceNew) {
      if (_filter == null) _filter = GetComponent<MeshFilter>();

      if (_filter.sharedMesh == null || forceNew) {
        Mesh mesh = new Mesh();
        mesh.name = "Generated Mesh " + _genCount++;
        _filter.sharedMesh = mesh;
      }

      _filter.sharedMesh.Clear();
      _vertCache.Clear();
      _idxCache.Clear();
      _normalCache.Clear();

      GenerateMeshInto(_vertCache, _idxCache, _normalCache);

      _filter.sharedMesh.SetVertices(_vertCache);
      _filter.sharedMesh.SetTriangles(_idxCache, 0, true);
      _filter.sharedMesh.SetNormals(_normalCache);
#if UNITY_EDITOR
    UnityEditor.Unwrapping.GenerateSecondaryUVSet(_filter.sharedMesh);
    _filter.sharedMesh.uv = _filter.sharedMesh.uv2;
#endif
      //_filter.sharedMesh.RecalculateNormals();
    }

    public abstract void GenerateMeshInto(List<Vector3> vertCache, List<int> indexCache, List<Vector3> normalCache);

  }

}