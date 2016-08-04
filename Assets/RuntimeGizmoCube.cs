using UnityEngine;
using Leap.Unity.RuntimeGizmos;

public class RuntimeGizmoCube : MonoBehaviour, IRuntimeGizmoDrawer {

  public void OnDrawRuntimeGizmos() {
    RGizmos.RelativeTo(transform);
    RGizmos.color = Color.green;
    RGizmos.DrawWireCube(Vector3.zero, Vector3.one);
  }
}
