using UnityEngine;
using System;
using InteractionEngine.Internal;

namespace InteractionEngine {

  public abstract class InteractionObject : MonoBehaviour {

    #region SERIALIZED FIELDS
    [SerializeField]
    protected InteractionController _controller;
    #endregion

    #region PUBLIC EVENTS
    public event Action<eLeapIEClassification> OnClassification;
    #endregion

    #region INTERNAL FIELDS
    private bool _hasRegisteredShapeDescription = false;
    private bool _isRegisteredWithController = false;

    protected LEAP_IE_SHAPE_DESCRIPTION_HANDLE _shapeHandle;
    #endregion

    #region PUBLIC METHODS
    public InteractionController Controller {
      get {
        return _controller;
      }
      set {
        _controller = value;
      }
    }

    public LEAP_IE_SHAPE_DESCRIPTION_HANDLE ShapeHandle {
      get {
        return _shapeHandle;
      }
    }

    public abstract LEAP_IE_TRANSFORM IeTransform {
      get;
      set;
    }

    public virtual void SetClassification(eLeapIEClassification classification) {
      if (OnClassification != null) {
        OnClassification(classification);
      }
    }

    public bool IsInteractionEnabled {
      get {
        return _isRegisteredWithController;
      }
      set {
        if (value) {
          EnableInteraction();
        } else {
          DisableInteraction();
        }
      }
    }

    public bool HasRegisteredShapeDescription {
      get {
        return _hasRegisteredShapeDescription;
      }
    }

    public void EnableInteraction() {
      if (_isRegisteredWithController) {
        return;
      }

      if (!_hasRegisteredShapeDescription) {
        throw new InvalidOperationException("Cannot enable interaction before a shape definition has been registered.");
      }

      _controller.RegisterInteractionObject(this);
      _isRegisteredWithController = true;
    }

    public void DisableInteraction() {
      if (!_isRegisteredWithController) {
        return;
      }

      _controller.UnregisterInteractionObject(this);
      _isRegisteredWithController = false;
    }

    public void Annotate(uint type, uint bytes, IntPtr data) {
      _controller.Annotate(this, type, bytes, data);
    }

    public void Annotate<T>(uint type, T t) where T : struct {
      _controller.Annotate(this, type, t);
    }
    #endregion

    #region PROTECTED METHODS
    protected void RegisterShapeDescription<T>(T shape) where T : struct {
      if (_hasRegisteredShapeDescription) {
        if (_isRegisteredWithController) {
          throw new InvalidOperationException("Cannot change the shape description while the object is registered with the controller.");
        }
        _controller.UnregisterShapeDescription(ref _shapeHandle);
      }

      IntPtr ptr = StructAllocator.AllocateStruct(shape);
      _shapeHandle = _controller.RegisterShapeDescription(ptr);
      _hasRegisteredShapeDescription = true;
    }
    #endregion
  }
}
