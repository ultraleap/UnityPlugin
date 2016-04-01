using UnityEngine;
using Leap.Unity.Interaction.CApi;
using LeapInternal;

namespace Leap.Unity.Interaction {

  public class InteractionBehaviourDebug : InteractionBehaviourBase {

    public override LEAP_IE_TRANSFORM InteractionTransform {
      get {
        LEAP_IE_TRANSFORM interactionTransform = new LEAP_IE_TRANSFORM();
        interactionTransform.position = new LEAP_VECTOR(transform.position);
        interactionTransform.rotation = new LEAP_QUATERNION(transform.rotation);
        return interactionTransform;
      }
    }

    public void OnDrawGizmos() {
      Bounds bounds = new Bounds(transform.position, Vector3.zero);
      foreach (Collider c in GetComponentsInChildren<Collider>()) {
        bounds.Encapsulate(c.bounds.min);
        bounds.Encapsulate(c.bounds.max);
      }

      if (IsBeingGrasped) {
        Gizmos.color = Color.green;
        Gizmos.DrawCube(bounds.center, bounds.size);
      } else {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
      }
    }

  }
}
