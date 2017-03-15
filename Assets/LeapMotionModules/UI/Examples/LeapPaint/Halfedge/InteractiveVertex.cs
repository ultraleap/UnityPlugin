using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Halfedge {

  public class InteractiveVertex : MonoBehaviour {

    public InteractiveMesh intMesh;

    private List<Vertex> _commonVertices = new List<Vertex>();
    public List<Vertex> commonVertices { get { return _commonVertices; } }

    public static InteractiveVertex Create(InteractiveMesh forMesh, List<Vertex> commonVerts, GameObject basePrefab = null) {
      GameObject intVertObj;
      if (basePrefab == null) {
        intVertObj = new GameObject("Interactive Vertex");
      }
      else {
        intVertObj = Instantiate<GameObject>(basePrefab);
      }

      intVertObj.transform.parent = forMesh.transform;
      var intVert = intVertObj.GetComponent<InteractiveVertex>() ?? intVertObj.AddComponent<InteractiveVertex>();
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
