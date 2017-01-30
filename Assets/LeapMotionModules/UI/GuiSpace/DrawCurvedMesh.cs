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
    foreach (var m in toDestroy) {
      DestroyImmediate(m);
    }

    Anchor a;
    a.rectPos = transform.position;
    a.anchorPos.radius = radius;
    a.anchorPos.height = 0;
    a.anchorPos.angle = 0;

    recurse(transform, a);
  }

  List<Mesh> toDestroy = new List<Mesh>();



  private void recurse(Transform t, Anchor anchor) {

    var da = t.GetComponent<AnchorOfConstantSize>();
    if (da != null && da.enabled) {
      var newAnchor = new Anchor();
      newAnchor.anchorPos = TransformPos(t.position, anchor);
      newAnchor.rectPos = t.position;

      anchor = newAnchor;
    }

    var r = t.GetComponent<Renderer>();
    if (r != null) {
      Mesh m = Instantiate(r.GetComponent<MeshFilter>().sharedMesh);
      toDestroy.Add(m);
      m.hideFlags = HideFlags.HideAndDontSave;

      var verts = m.vertices;
      for (int i = 0; i < verts.Length; i++) {
        Vector3 vert = verts[i];
        Vector3 worldVert = t.TransformPoint(vert);

        var radialVert = TransformPos(worldVert, anchor);
        var transformedVert = RadialPosToWorld(radialVert);

        verts[i] = transformedVert;
      }

      m.vertices = verts;
      m.bounds = new Bounds(Vector3.zero, Vector3.one * 100000);
      m.RecalculateNormals();

      Graphics.DrawMesh(m, transform.localToWorldMatrix, r.sharedMaterial, 0);
    }

    foreach (Transform child in t) {
      if (!child.gameObject.activeSelf) continue;
      recurse(child, anchor);
    }
  }

  public RadialPos TransformPos(Vector3 worldPos, Anchor anchor) {
    RadialPos rp = anchor.anchorPos;
    Vector3 delta = worldPos - anchor.rectPos;
    rp.angle += delta.x / rp.radius;
    rp.height += delta.y;
    rp.radius += delta.z;

    return rp;
  }

  public Vector3 RadialPosToWorld(RadialPos pos) {
    Vector3 worldPos;
    worldPos.x = Mathf.Sin(pos.angle) * pos.radius;
    worldPos.y = pos.height;
    worldPos.z = Mathf.Cos(pos.angle) * pos.radius - radius;
    return worldPos;
  }

  public struct Anchor {
    public Vector3 rectPos;
    public RadialPos anchorPos;
  }

  public struct RadialPos {
    public float radius;
    public float angle;
    public float height;
  }
}
