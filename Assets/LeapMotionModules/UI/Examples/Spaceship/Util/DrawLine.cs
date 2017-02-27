using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawLine : MonoBehaviour, IRuntimeGizmoComponent {

  public Color color = Color.white;
  public GameObject target = null;

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    if (target != null) {
      drawer.color = color;
      drawer.DrawLine(this.transform.position, target.transform.position);
    }
  }
}
