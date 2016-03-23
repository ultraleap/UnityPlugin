using UnityEngine;
using InteractionEngine.CApi;

namespace InteractionEngine {

  public class TestInteractionObject : InteractionObject {
    private SphereCollider _sphereCollider;
    private Renderer _renderer;

    public override LEAP_IE_SHAPE_DESCRIPTION_HANDLE GetShapeDescription() {
      return _controller.ShapePool.GetSphere(_sphereCollider.radius * transform.lossyScale.x);
    }

    protected override void OnGraspEnterFirst(int handId) {
      base.OnGraspEnterFirst(handId);
      _renderer.material.color = Color.green;
    }

    protected override void OnGraspExitLast(int handId) {
      base.OnGraspExitLast(handId);
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
