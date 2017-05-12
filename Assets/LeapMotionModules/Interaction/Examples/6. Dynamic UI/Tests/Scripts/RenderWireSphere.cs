using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  [AddComponentMenu("")]
  public class RenderWireSphere : MonoBehaviour, IRuntimeGizmoComponent {

    public float radius = 0.30F;
    public Color color = Color.red;

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (!gameObject.active) return;

      drawer.color = color;
      drawer.DrawWireSphere(this.transform.position, radius);
    }

  }

}
