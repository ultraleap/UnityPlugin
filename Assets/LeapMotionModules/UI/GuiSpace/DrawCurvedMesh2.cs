using UnityEngine;

[ExecuteInEditMode]
public class DrawCurvedMesh2 : MonoBehaviour {
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
    recurse(transform);
  }

  private void recurse(Transform t) {
    var r = t.GetComponent<Renderer>();
    if (r != null) {
      Mesh m = Instantiate(r.GetComponent<MeshFilter>().sharedMesh);
      m.hideFlags = HideFlags.HideAndDontSave;

      var verts = m.vertices;
      for (int i = 0; i < verts.Length; i++) {
        Vector3 vert = verts[i];
        Vector3 delta = t.root.InverseTransformPoint(t.TransformPoint(vert));

        float vertRadius = radius + delta.z;
        float vertHeight = delta.y;
        float vertAngle = delta.x / (transform.GetChild(0).localPosition.z + radius);

        Vector3 v;
        v.x = Mathf.Sin(vertAngle) * vertRadius;
        v.y = vertHeight;
        v.z = Mathf.Cos(vertAngle) * vertRadius - radius;

        verts[i] = v;
      }

      m.vertices = verts;
      m.bounds = new Bounds(Vector3.zero, Vector3.one * 100000);
      m.RecalculateNormals();

      Graphics.DrawMesh(m, transform.root.localToWorldMatrix, r.sharedMaterial, 0);
    }

    foreach (Transform child in t) {
      recurse(child);
    }
  }
}