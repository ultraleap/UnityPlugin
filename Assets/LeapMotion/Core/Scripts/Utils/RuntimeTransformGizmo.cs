using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.RuntimeGizmos {

  public class RuntimeTransformGizmo : MonoBehaviour, IRuntimeGizmoComponent {

    private void Start() { }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (!this.enabled || !this.gameObject.activeInHierarchy) return;

      drawer.DrawPose(this.transform.ToPose(), this.transform.lossyScale.x * 0.05f,
                      drawCube: false);
    }

  }

}
