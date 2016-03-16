using UnityEngine;
using System;
using System.Collections.Generic;
using InteractionEngine.CApi;

namespace InteractionEngine {

  public abstract class InteractionObject : MonoBehaviour {

    #region SERIALIZED FIELDS
    [SerializeField]
    protected InteractionController _controller;
    #endregion

    #region PUBLIC EVENTS
    public event Action<int> OnGraspEvent;
    public event Action<int> OnReleaseEvent;
    public event Action<int> OnFirstGraspEvent;
    public event Action<int> OnLastReleaseEvent;
    #endregion

    #region INTERNAL FIELDS
    private bool _hasRegisteredShapeDescription = false;
    private bool _isRegisteredWithController = false;

    private List<int> _graspingIds = new List<int>();

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
    /// Returns true if there is at least one hand grasping this object.
    /// </summary>
    public bool IsBeingGrasped {
      get {
        return _graspingIds.Count > 0;
      }
    }

    /// <summary>
    /// Returns the number of hands that are currently grasping this object.
    /// </summary>
    public int GraspingHandCount {
      get {
        return _graspingIds.Count;
      }
    }

    /// <summary>
    /// Returns the ids of the hands currently grasping this object.
    /// </summary>
    public IEnumerable<int> GraspingHands {
      get {
        return _graspingIds;
      }
    }

    public abstract LEAP_IE_TRANSFORM GetIETransform();
    public abstract LEAP_IE_SHAPE_DESCRIPTION_HANDLE GetShapeDescription();

    public bool IsBeingGraspedByHand(int handId) {
      return _graspingIds.Contains(handId);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handId"></param>
    public virtual void BeginHandGrasp(int handId) {
      if (_graspingIds.Contains(handId)) {
        throw new InvalidOperationException("Cannot BeginGrasp with hand id " + handId +
                                            " because a hand of that id is already grasping this object.");
      }

      _graspingIds.Add(handId);

      if (OnGraspEvent != null) {
        OnGraspEvent(handId);
      }
      if (_graspingIds.Count == 1) {
        OnFirstGrasp(handId);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handId"></param>
    public virtual void EndHandGrasp(int handId) {
      if (_graspingIds.Count == 0) {
        throw new InvalidOperationException("Cannot EndGrasp with hand id " + handId +
                                            " because there are no hands current grasping this object.");
      }

      if (!_graspingIds.Contains(handId)) {
        throw new InvalidOperationException("Cannot EndGrasp with hand id " + handId +
                                            " because a hand of that id not already grasping this object.");
      }

      _graspingIds.Remove(handId);

      if (_graspingIds.Count == 0) {
        OnLastRelease(handId);
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
    protected virtual void OnFirstGrasp(int handId) {
      if (OnFirstGraspEvent != null) {
        OnFirstGraspEvent(handId);
      }
    }

    protected virtual void OnLastRelease(int handId) {
      if (OnLastReleaseEvent != null) {
        OnLastReleaseEvent(handId);
      }
    }
    #endregion
  }
}
