using Leap.Unity.Query;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Halfedge {

  public class InteractiveFace : MonoBehaviour {

    public InteractiveMesh intMesh;

    public Face face;

    public static InteractiveFace Create(InteractiveMesh forMesh, Face face, GameObject basePrefab = null) {
      GameObject intFaceObj;
      if (basePrefab == null) {
        intFaceObj = new GameObject("Interactive Vertex");
      }
      else {
        intFaceObj = Instantiate<GameObject>(basePrefab);
      }

      intFaceObj.transform.parent = forMesh.transform;
      int count = 0;
      Vector3 pos = Vector3.zero;
      face.vertices.Query().Select(v => { pos += v.position; count++; });
      intFaceObj.transform.localPosition = 
      
      var intVert = intFaceObj.GetComponent<InteractiveVertex>() ?? intFaceObj.AddComponent<InteractiveVertex>();
      intVert.intMesh = forMesh;
      intVert.SetCommonVertices(commonVerts);
      return intVert;
    }

    public void SetCommonVertices(List<Vertex> commonVerts) {
      _commonVertices.Clear();
      foreach (var vert in commonVerts) {
        _commonVertices.Add(vert);
      }
      if (_commonVertices.Count > 0) {
        this.transform.position = intMesh.transform.TransformPoint(commonVerts[0].position);
      }
    }

    void Update() {
      if (this.transform.hasChanged) {
        foreach (var vert in _commonVertices) {
          vert.position = intMesh.transform.InverseTransformPoint(this.transform.position);
        }
        intMesh.RebuildUnityMeshData();
        this.transform.hasChanged = false;
      }
    }

  }

}
