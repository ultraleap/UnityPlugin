using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Attachments {

  public class Anchor : MonoBehaviour, IRuntimeGizmoComponent {

    public float anchorRange = 0.1F;

    public bool IsWithinRange(Vector3 position) {
      Vector3 delta = this.transform.position - position;
      return delta.sqrMagnitude < anchorRange * anchorRange;
    }

    /// <summary> Returns the squared distance to the target. AnchorGroups use this to avoid
    /// square roots while determining the closest anchor to a given position. </summary>
    internal float GetDistanceSqrd(Vector3 position) {
      Vector3 delta = this.transform.position - position;
      return delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
    }

    public bool drawRangeGizmo = false;
    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (!drawRangeGizmo) return;
      drawer.color = new Color(0.4F, 0.5F, 0.15F);
      drawer.DrawWireSphere(this.transform.position, anchorRange);
    }

  }

}