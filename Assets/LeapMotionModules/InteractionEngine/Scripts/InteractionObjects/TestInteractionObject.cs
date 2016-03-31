using UnityEngine;
using Leap.Unity.Interaction.CApi;
using LeapInternal;

namespace Leap.Unity.Interaction {

  public class TestInteractionObject : InteractionBehaviourBase {
    private Renderer _renderer;

    public override LEAP_IE_TRANSFORM InteractionTransform {
      get {
        LEAP_IE_TRANSFORM interactionTransform = new LEAP_IE_TRANSFORM();
        interactionTransform.position = new LEAP_VECTOR(transform.position);
        interactionTransform.rotation = new LEAP_QUATERNION(transform.rotation);
        return interactionTransform;
      }
    }

    protected override void OnGraspBegin() {
      base.OnGraspBegin();
      _renderer.material.color = Color.green;
    }

    protected override void OnGraspEnd() {
      base.OnGraspEnd();
      _renderer.material.color = Color.white;
    }

    void Awake() {
      _renderer = GetComponent<Renderer>();
    }
  }
}
