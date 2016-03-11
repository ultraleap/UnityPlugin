using UnityEngine;
using System;
using InteractionEngine.CApi;

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
    /// <summary>
    /// Gets or sets the controller this object belongs to.  If this object already has interaction enabled, changing the 
    /// controller will disable interaction and require the shape to be redefined. 
    /// </summary>
    public InteractionController Controller {
      get {
        return _controller;
      }
      set {
        if (_controller != value) {
          if (IsInteractionEnabled) {
            DisableInteraction();
          }

          _hasRegisteredShapeDescription = false;
          _controller = value;
        }
      }
    }

    /// <summary>
    /// Gets the internal handle to the description of the shape of this object.
    /// </summary>
    public LEAP_IE_SHAPE_DESCRIPTION_HANDLE ShapeHandle {
      get {
        return _shapeHandle;
      }
    }


    /// <summary>
    /// Gets or Sets the position of this object using the internal transform struct.
    /// </summary>
    public abstract LEAP_IE_TRANSFORM IeTransform {
      get;
      set;
    }

    /// <summary>
    /// Called by the controller when the classification for this object changes.
    /// </summary>
    /// <param name="classification"></param>
    public virtual void SetClassification(eLeapIEClassification classification) {
      if (OnClassification != null) {
        OnClassification(classification);
      }
    }

    /// <summary>
    /// Gets or Sets whether or not interaction is enabled for this object.  Setting is 
    /// equivilent to calling EnableInteraction() or DisableInteraction()
    /// </summary>
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

    /// <summary>
    /// Gets whether or not this object has registered it's shape with it's controller.  This can and must
    /// be true before interaction is enabled.
    /// </summary>
    public bool HasRegisteredShapeDescription {
      get {
        return _hasRegisteredShapeDescription;
      }
    }

    /// <summary>
    /// Calling this method registers this object with the controller.  A shape definition must be registered
    /// with the controller before interaction can be enabled.
    /// </summary>
    public void EnableInteraction() {
      if (_controller == null) {
        throw new InvalidOperationException("Cannot enable interaction until a controller has been set");
      }

      if (_isRegisteredWithController) {
        return;
      }

      if (!_hasRegisteredShapeDescription) {
        throw new InvalidOperationException("Cannot enable interaction before a shape definition has been registered.");
      }

      _controller.RegisterInteractionObject(this);
      _isRegisteredWithController = true;
    }

    /// <summary>
    /// Calling this method will unregister this object from the controller.  The shape definition remains 
    /// registered and does not need to be re-registered if interaction is enabled again.  
    /// </summary>
    public void DisableInteraction() {
      if (!_isRegisteredWithController) {
        return;
      }

      if (_controller == null) {
        throw new InvalidOperationException("Cannot disable interaction until a controller has been set");
      }

      _controller.UnregisterInteractionObject(this);
      _isRegisteredWithController = false;
    }
    #endregion

    #region PROTECTED METHODS
    /// <summary>
    /// Call this method to register a custom shape definiton with the controller.  A second shape description cannot
    /// be registered once a first description has already been registered.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="shape"></param>
    protected void RegisterShapeDescription<T>(T shape) where T : struct {
      if (_controller == null) {
        throw new InvalidOperationException("Cannot register a shape description before setting the controller.");
      }

      if (_hasRegisteredShapeDescription) {
        throw new InvalidOperationException("Cannot change the shape description once it has been registered with a controller.");
      }

      IntPtr ptr = StructAllocator.AllocateStruct(shape);
      _shapeHandle = _controller.RegisterShapeDescription(ptr);
      _hasRegisteredShapeDescription = true;
    }

    protected void AutoGenerateShapeDescription() {
      RegisterShapeDescription(new LEAP_IE_COMPOUND_DESCRIPTION(gameObject));
    }

    #endregion
  }
}
