using UnityEngine;
using LeapInternal;
using InteractionEngine.CApi;
using System;

namespace InteractionEngine {

  public class TestInteractionObject : InteractionObject {
    private SphereCollider _sphereCollider;
    private Renderer _renderer;

    public override LEAP_IE_TRANSFORM GetIETransform() {
      LEAP_IE_TRANSFORM ieTransform = new LEAP_IE_TRANSFORM();
      ieTransform.position = new LEAP_VECTOR(transform.position);
      ieTransform.rotation = new LEAP_QUATERNION(transform.rotation);
      return ieTransform;
    }

    public override LEAP_IE_SHAPE_DESCRIPTION_HANDLE GetShapeDescription() {
      return _controller.ShapePool.GetSphere(_sphereCollider.radius);
    }

    protected override void OnFirstGrasp(int handId) {
      base.OnFirstGrasp(handId);
      _renderer.material.color = Color.green;
    }

    protected override void OnLastRelease(int handId) {
      base.OnLastRelease(handId);
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
