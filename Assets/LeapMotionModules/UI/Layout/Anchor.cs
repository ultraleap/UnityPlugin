using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Layout {

  public class Anchor : MonoBehaviour {

    public float anchorRange = 0.1F;

    public bool IsWithinRange(Vector3 position) {
      Vector3 delta = this.transform.position - position;
      return delta.x * delta.x + delta.y * delta.y + delta.z * delta.z < anchorRange * anchorRange;
    }

    public bool drawRangeGizmo = false;
    void OnDrawGizmos() {
      if (!drawRangeGizmo) return;
      Gizmos.color = new Color(0.4F, 0.5F, 0.15F);
      Gizmos.DrawWireSphere(this.transform.position, anchorRange);
    }

  }

}