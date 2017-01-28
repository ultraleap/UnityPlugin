using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DrawCurvedMesh : MonoBehaviour {

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
    p.angle = 0;
    p.height = 0;

    Anchor a;
    a.transform = transform;
    a.anchorPos.radius = radius;
    a.anchorPos.height = 0;
    a.anchorPos.angle = 0;

    recurse(transform, a);
  }

  private struct Anchor {
    public Transform transform;
    public RadialPos anchorPos;
  }

  private void recurse(Transform t, Anchor anchor) {

    var da = t.GetComponent<AnchorOfConstantSize>();
    if (da != null && da.enabled) {
      Vector3 delta = t.position - anchor.transform.position;

      var newAnchor = new Anchor();
      newAnchor.anchorPos = anchor.anchorPos;
      newAnchor.anchorPos.angle += delta.x / anchor.anchorPos.radius;
      newAnchor.anchorPos.height += delta.y;
      newAnchor.anchorPos.radius += delta.z;

      newAnchor.transform = t;

      anchor = newAnchor;
    }

    var r = t.GetComponent<Renderer>();
    if (r != null) {
      Mesh m = Instantiate(r.GetComponent<MeshFilter>().sharedMesh);
      m.hideFlags = HideFlags.HideAndDontSave;

      var verts = m.vertices;
      for (int i = 0; i < verts.Length; i++) {
        Vector3 vert = verts[i];
        Vector3 delta = t.TransformPoint(vert) - anchor.transform.position;

        RadialPos vertPos = anchor.anchorPos;
        vertPos.angle += delta.x / vertPos.radius;
        vertPos.radius += delta.z;
        vertPos.height += delta.y;

        Vector3 v;
        v.x = Mathf.Sin(vertPos.angle) * vertPos.radius;
        v.y = vertPos.height;
        v.z = Mathf.Cos(vertPos.angle) * vertPos.radius - radius;

        verts[i] = v;
      }

      m.vertices = verts;
      m.bounds = new Bounds(Vector3.zero, Vector3.one * 100000);
      m.RecalculateNormals();

      Graphics.DrawMesh(m, transform.localToWorldMatrix, r.sharedMaterial, 0);
    }

    foreach (Transform child in t) {
      Vector3 delta = child.position - anchor.transform.position;

      RadialPos childPos = anchor.anchorPos;
      childPos.angle += delta.x / childPos.radius;
      childPos.radius += delta.z;
      childPos.height += delta.y;

      recurse(child, anchor);
    }
  }

  public struct RadialPos {
    public float radius;
    public float angle;
    public float height;
  }
}
