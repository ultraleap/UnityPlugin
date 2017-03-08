using Leap.Unity.Attributes;
using Leap.Unity.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class SimpleMeshGenWrapper : MonoBehaviour {

  public bool newMeshNow = false;
  private bool _newMeshNowState = false;

  [MinValue(0)]
  public Vector3 extents = new Vector3(1F, 1F, 0.5F);
  [MinValue(0)]
  public float cornerRadius = 0.2F;
  [MinValue(0)]
  public int cornerDivisions = 5;
  public bool withBack = true;

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
  private void InitMesh(bool forceNew) {
    if (_filter == null) _filter = GetComponent<MeshFilter>();

    if (_filter.sharedMesh == null || forceNew) {
//#if UNITY_EDITOR
//      DestroyImmediate(_filter.sharedMesh, true);
//#endif
      Mesh mesh = new Mesh();
      mesh.name = "Generated Mesh " + _genCount++;
      _filter.sharedMesh = mesh;
    }

    _filter.sharedMesh.Clear();
    _vertCache.Clear();
    _idxCache.Clear();
    MeshGen.GenerateRoundedRectPrism(extents, cornerRadius, cornerDivisions, _vertCache, _idxCache, withBack);
    _filter.sharedMesh.SetVertices(_vertCache);
    _filter.sharedMesh.SetTriangles(_idxCache, 0, true);
#if UNITY_EDITOR
    UnityEditor.Unwrapping.GenerateSecondaryUVSet(_filter.sharedMesh);
    _filter.sharedMesh.uv = _filter.sharedMesh.uv2;
#endif
    _filter.sharedMesh.RecalculateNormals();
  }

}
