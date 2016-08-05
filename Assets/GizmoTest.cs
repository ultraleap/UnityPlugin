using UnityEngine;
using System.Collections;
using Leap.Unity.RuntimeGizmos;
using System;

public class GizmoTest : MonoBehaviour, IRuntimeGizmoComponent {

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    drawer.color = Color.green;
    drawer.DrawColliders(gameObject, useWireframe: false);
  }
}
