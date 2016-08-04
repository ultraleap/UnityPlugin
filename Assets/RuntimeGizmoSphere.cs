using UnityEngine;
using System.Collections;
using Leap.Unity.RuntimeGizmos;
using System;

public class RuntimeGizmoSphere : MonoBehaviour, IRuntimeGizmoDrawer {

  public void OnDrawRuntimeGizmos() {
    RGizmos.RelativeTo(transform);
    RGizmos.color = Color.blue;
    RGizmos.DrawWireSphere(Vector3.zero, 0.5f);
  }
}
