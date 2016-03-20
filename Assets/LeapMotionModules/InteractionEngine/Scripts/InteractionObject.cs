using UnityEngine;
using System;
using System.Collections.Generic;
using Leap;
using LeapInternal;
using InteractionEngine.CApi;

namespace InteractionEngine {

  public abstract class InteractionObject : MonoBehaviour {

    #region SERIALIZED FIELDS
    [SerializeField]
    protected InteractionController _controller;
    #endregion

    #region PUBLIC EVENTS
    public event Action<int> OnGraspEnterEvent;
    public event Action<int[]> OnGraspStayEvent;
    public event Action<int> OnGraspExitEvent;

    public event Action<int> OnGraspEnterFirstEvent;
    public event Action<int> OnGraspExitLastEvent;
    #endregion

    #region INTERNAL FIELDS
    private bool _isRegisteredWithController = false;

    private List<int> _graspingIds = new List<int>();

    protected LEAP_IE_SHAPE_DESCRIPTION_HANDLE _shapeHandle;
    #endregion

    #region PUBLIC METHODS
    /// <summary>
    /// Gets or sets the controller this object belongs to.  
    /// </summary>
    public InteractionController Controller {
      get {
        return _controller;
      }
      set {
        if (_controller != value) {
          if (IsInteractionEnabled) {
            DisableInteraction();
            _controller = value;
            EnableInteraction();
          } else {
            _controller = value;
          }
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

    /// <summary>
    /// Returns the internal transform representation of this object.
    /// </summary>
    /// <returns></returns>
    public virtual LEAP_IE_TRANSFORM GetIETransform() {
      LEAP_IE_TRANSFORM ieTransform = new LEAP_IE_TRANSFORM();
      ieTransform.position = new LEAP_VECTOR(transform.position);
      ieTransform.rotation = new LEAP_QUATERNION(transform.rotation);
      return ieTransform;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public abstract LEAP_IE_SHAPE_DESCRIPTION_HANDLE GetShapeDescription();

    /// <summary>
    /// Returns whether or not a hand with the given ID is currently grasping this object.
    /// </summary>
    /// <param name="handId"></param>
    /// <returns></returns>
    public bool IsBeingGraspedByHand(int handId) {
      return _graspingIds.Contains(handId);
    }

    /// <summary>
    /// Called by InteractionController when a Hand begins grasping this object.
    /// </summary>
    /// <param name="handId"></param>
    public virtual void OnGraspEnter(int handId) {
      if (_graspingIds.Contains(handId)) {
        throw new InvalidOperationException("Cannot BeginGrasp with hand id " + handId +
                                            " because a hand of that id is already grasping this object.");
      }

      _graspingIds.Add(handId);

      if (OnGraspEnterEvent != null) {
        OnGraspEnterEvent(handId);
      }
      if (_graspingIds.Count == 1) {
        OnGraspEnterFirst(handId);
      }
    }

    public virtual void OnGraspStay(Hand[] graspingHands) {

    }

    /// <summary>
    /// Called by InteractionController when a Hand stops grasping this object.
    /// </summary>
    /// <param name="handId"></param>
    public virtual void OnGraspExit(int handId) {
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
        OnGraspExitLast(handId);
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

      //InteractionController does not dispatch EndHandGrasp events during unregistration
      //We dispatch them ourselves!
      for (int i = _graspingIds.Count - 1; i >= 0; i--) {
        OnGraspExit(_graspingIds[i]);
      }
    }
    #endregion

    #region PROTECTED METHODS
    protected virtual void OnGraspEnterFirst(int handId) {
      if (OnGraspEnterFirstEvent != null) {
        OnGraspEnterFirstEvent(handId);
      }
    }

    protected virtual void OnGraspExitLast(int handId) {
      if (OnGraspExitLastEvent != null) {
        OnGraspExitLastEvent(handId);
      }
    }
    #endregion
  }
}
