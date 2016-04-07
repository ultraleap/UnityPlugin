using UnityEngine;
using Leap.Unity.Interaction.CApi;
using LeapInternal;
using System;

namespace Leap.Unity.Interaction {

  public class InteractionBehaviourDebug : InteractionBehaviourBase {

    public override INTERACTION_TRANSFORM InteractionTransform {
      get {
        INTERACTION_TRANSFORM interactionTransform = new INTERACTION_TRANSFORM();
        interactionTransform.position = transform.position.ToCVector();
        interactionTransform.rotation = transform.rotation.ToCQuaternion();
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
