using UnityEngine;
using System;
using LeapInternal;
using InteractionEngine.Internal;

namespace InteractionEngine {

  public abstract class InteractionObject : MonoBehaviour {
    [SerializeField]
    private InteractionController _controller;

    public abstract IntPtr ShapeDescription {
      get;
    }

    public abstract LEAP_IE_TRANSFORM IeTransform {
      get;
      set;
    }

    public abstract void SetClassification(eLeapIEClassification classification);

    protected virtual void OnValidate() {
      if (_controller == null) {
        _controller = FindObjectOfType<InteractionController>();
      }
    }

    protected virtual void Awake() { }

    protected virtual void Start() { }

    protected virtual void OnEnable() {
      _controller.RegisterInteractionObject(this);
    }

    protected virtual void OnDisable() {
      _controller.UnregisterInteractionObject(this);
    }

    protected virtual void Update() { }
  }
}
