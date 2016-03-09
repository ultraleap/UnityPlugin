using UnityEngine;
using System;
using LeapInternal;
using InteractionEngine.Internal;

namespace InteractionEngine {

  public abstract class InteractionObject : MonoBehaviour {
    [SerializeField]
    protected InteractionController _controller;

    protected LEAP_IE_SHAPE_DESCRIPTION_HANDLE _shapeHandle;

    public LEAP_IE_SHAPE_DESCRIPTION_HANDLE ShapeHandle {
      get {
        return _shapeHandle;
      }
    }

    public abstract LEAP_IE_TRANSFORM IeTransform {
      get;
      set;
    }

    public abstract void SetClassification(eLeapIEClassification classification);

    protected abstract IntPtr allocateShapeDescription();

    protected virtual void Reset() {
      _controller = FindObjectOfType<InteractionController>();
    }

    protected virtual void OnValidate() {
      if (_controller == null) {
        _controller = FindObjectOfType<InteractionController>();
      }
    }

    protected virtual void Awake() {
      //Must be in Awake so it happens before OnEnable
      IntPtr shapeDesc = allocateShapeDescription();
      _shapeHandle = _controller.RegisterShapeDescription(shapeDesc);
    }

    protected virtual void Start() { }

    protected virtual void OnEnable() {
      if (_controller != null) {
        _controller.RegisterInteractionObject(this);
      }
    }

    protected virtual void OnDisable() {
      if (_controller != null) {
        _controller.UnregisterInteractionObject(this);
      }
    }

    protected virtual void Update() { }
  }
}
