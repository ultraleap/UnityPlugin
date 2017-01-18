using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DrawCurvedMesh3 : MonoBehaviour {

  void OnEnable() {
    foreach (var r in GetComponentsInChildren<Renderer>()) {
      r.enabled = false;
    }
  }

  void OnDisable() {
    foreach (var r in GetComponentsInChildren<Renderer>()) {
      r.enabled = true;
    }
  }

  public float radius = 1;
  void Update() {
    RadialPos p;
    p.radius = radius;
    p.angleX = 0;
    p.angleY = 0;
    recurse(transform, p);
  }

  private void recurse(Transform t, RadialPos pos) {
    var r = t.GetComponent<Renderer>();
    if (r != null) {
      Mesh m = Instantiate(r.GetComponent<MeshFilter>().sharedMesh);
      m.hideFlags = HideFlags.HideAndDontSave;

      var verts = m.vertices;
      for (int i = 0; i < verts.Length; i++) {
        Vector3 vert = verts[i];
        Vector3 delta = t.TransformPoint(vert) - t.position;

        RadialPos vertPos;
        vertPos.radius = pos.radius + delta.z;
        vertPos.angleX = pos.angleX + delta.y / pos.radius;
        vertPos.angleY = pos.angleY + delta.x / pos.radius;

        Vector3 v;
        v.x = 0;
        v.y = Mathf.Sin(vertPos.angleX) * vertPos.radius;
        v.z = Mathf.Cos(vertPos.angleX) * vertPos.radius;

        Vector3 v1 = v;
        v1.x = v.z * Mathf.Sin(vertPos.angleY);
        v1.y = v.y;
        v1.z = v.z * Mathf.Cos(vertPos.angleY);

        verts[i] = v1 - Vector3.forward * radius;
      }

      m.vertices = verts;
      m.bounds = new Bounds(Vector3.zero, Vector3.one * 100000);
      m.RecalculateNormals();

      Graphics.DrawMesh(m, transform.localToWorldMatrix, r.sharedMaterial, 0);
    }

    foreach (Transform child in t) {
      Vector3 delta = child.position - t.position;

      RadialPos childPos;
      childPos.radius = pos.radius + delta.z;
      childPos.angleX = pos.angleX + delta.y / pos.radius;
      childPos.angleY = pos.angleY + delta.x / pos.radius;

      recurse(child, childPos);
    }
  }

  public struct RadialPos {
    public float radius;
    public float angleX;
    public float angleY;
  }
}
