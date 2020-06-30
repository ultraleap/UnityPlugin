using UnityEngine;
public class Rope : MonoBehaviour {
  public Vector3 localThisObj, localConnectedObj;
  public Transform connectedObj;
  LineRenderer lineRenderer;
  void Start() {
    lineRenderer = GetComponent<LineRenderer>();
  }
  void Update() {
    lineRenderer.SetPosition(0, transform.TransformPoint(localThisObj));
    lineRenderer.SetPosition(1, connectedObj.TransformPoint(localConnectedObj));
  }
}
