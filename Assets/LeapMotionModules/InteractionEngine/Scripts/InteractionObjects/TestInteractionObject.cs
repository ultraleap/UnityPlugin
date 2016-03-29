using UnityEngine;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  public class TestInteractionObject : InteractionBehaviour {
    private SphereCollider _sphereCollider;
    private Renderer _renderer;

    public override LEAP_IE_SHAPE_DESCRIPTION_HANDLE GetShapeDescription() {
      return _controller.ShapePool.GetSphere(_sphereCollider.radius * transform.lossyScale.x);
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
      _sphereCollider = GetComponent<SphereCollider>();
    }

    void OnEnable() {
      EnableInteraction();
    }

    void OnDisable() {
      DisableInteraction();
    }
  }
}
