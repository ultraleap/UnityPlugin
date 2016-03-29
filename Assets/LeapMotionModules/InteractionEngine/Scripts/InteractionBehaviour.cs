using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;
using LeapInternal;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  [DisallowMultipleComponent]
  public abstract class InteractionBehaviour : MonoBehaviour {

    #region SERIALIZED FIELDS
    [SerializeField]
    protected InteractionManager _manager;
    #endregion

    #region PUBLIC EVENTS
    public event Action<Hand> OnHandGraspEvent;
    public event Action<List<Hand>> OnHandsHoldEvent;
    public event Action<Hand> OnHandReleaseEvent;

    public event Action<Hand> OnHandLostTrackingEvent;
    public event Action<Hand, int> OnHandRegainedTrackingEvent;
    public event Action<int> OnHandTimeoutEvent;

    public event Action OnGraspBeginEvent;
    public event Action OnGraspEndEvent;
    #endregion

    #region INTERNAL FIELDS
    private bool _isRegisteredWithManager = false;

    private bool _hasShapeDescriptionBeenCreated = false;
    private LEAP_IE_SHAPE_DESCRIPTION_HANDLE _shapeDescriptionHandle;
    private LEAP_IE_SHAPE_INSTANCE_HANDLE _shapeInstanceHandle;

    private List<int> _graspingIds = new List<int>();
    private List<int> _untrackedIds = new List<int>();
    #endregion

    #region PUBLIC METHODS
    /// <summary>
    /// Gets or sets the manager this object belongs to.  
    /// </summary>
    public InteractionManager Manager {
      get {
        return _manager;
      }
      set {
        if (_manager != value) {
          if (IsInteractionEnabled) {
            DisableInteraction();
            _manager = value;
            EnableInteraction();
          } else {
            _manager = value;
          }
        }
      }
    }

    /// <summary>
    /// Gets or Sets whether or not interaction is enabled for this object.  Setting is 
    /// equivilent to calling EnableInteraction() or DisableInteraction()
    /// </summary>
    public bool IsInteractionEnabled {
      get {
        return _isRegisteredWithManager;
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
    /// Gets the shape description handle 
    /// </summary>
    public LEAP_IE_SHAPE_DESCRIPTION_HANDLE ShapeDescriptionHandle {
      get {
        if (_isRegisteredWithManager) {
          throw new NotRegisteredWithManagerException();
        }

        if (!_hasShapeDescriptionBeenCreated) {
          _shapeDescriptionHandle = GenerateShapeDescriptionHandle();
          _hasShapeDescriptionBeenCreated = true;
        }
        return _shapeDescriptionHandle;
      }
    }

    public LEAP_IE_SHAPE_INSTANCE_HANDLE ShapeInstanceHandle {
      get {
        if (_isRegisteredWithManager) {
          throw new NotRegisteredWithManagerException();
        }

        return _shapeInstanceHandle;
      }
    }

    public LEAP_IE_TRANSFORM InteractionTransform {
      get {
        LEAP_IE_TRANSFORM interactionTransform = new LEAP_IE_TRANSFORM();
        interactionTransform.position = new LEAP_VECTOR(transform.position);
        interactionTransform.rotation = new LEAP_QUATERNION(transform.rotation);
        return interactionTransform;
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
    /// Returns the number of hands that are currently grasping this object but
    /// are not currently being tracked.
    /// </summary>
    public int UntrackedHandCount {
      get {
        return _untrackedIds.Count;
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
    /// Returns the ids of the hands currently grasping this object, but only
    /// returns ids of hands that are also currently being tracked.
    /// </summary>
    public IEnumerable<int> TrackedGraspingHands {
      get {
        for (int i = 0; i < _graspingIds.Count; i++) {
          int id = _graspingIds[i];
          if (!_untrackedIds.Contains(id)) {
            yield return id;
          }
        }
      }
    }

    /// <summary>
    /// Returns the ids of the hands that are considered grasping but are untracked.
    /// </summary>
    public IEnumerable<int> UntrackedGraspingHands {
      get {
        return _untrackedIds;
      }
    }

    /// <summary>
    /// Returns whether or not a hand with the given ID is currently grasping this object.
    /// </summary>
    /// <param name="handId"></param>
    /// <returns></returns>
    public bool IsBeingGraspedByHand(int handId) {
      return _graspingIds.Contains(handId);
    }

    /// <summary>
    /// Called by InteractionManager when the interaction shape associated with this InteractionBehaviour
    /// is created and added to the interaction scene.
    /// </summary>
    /// <param name="instanceHandle"></param>
    public virtual void OnInteractionShapeCreated(LEAP_IE_SHAPE_INSTANCE_HANDLE instanceHandle) {
      _shapeInstanceHandle = instanceHandle;
    }

    /// <summary>
    /// Called by InteractionManager when the interaction shape associated with this InteractionBehaviour
    /// is destroyed and removed from the interaction scene.
    /// </summary>
    public virtual void OnInteractionShapeDestroyed() {
      _shapeInstanceHandle = new LEAP_IE_SHAPE_INSTANCE_HANDLE();
      _shapeDescriptionHandle = new LEAP_IE_SHAPE_DESCRIPTION_HANDLE();
      _hasShapeDescriptionBeenCreated = false;
    }

    /// <summary>
    /// Called by InteractionManager when this object is pushed.  The arguments are the proposed
    /// linear and angular velocities to be applied to the object. 
    /// </summary>
    /// <param name="linearVelocity"></param>
    /// <param name="angularVelocity"></param>
    public virtual void OnPush(Vector3 linearVelocity, Vector3 angularVelocity) { }

    /// <summary>
    /// Called by InteractionManager when a Hand begins grasping this object.
    /// </summary>
    /// <param name="handId"></param>
    public virtual void OnHandGrasp(Hand hand) {
      Assert.IsFalse(_graspingIds.Contains(hand.Id), HandAlreadyGraspingMessage(hand.Id));

      _graspingIds.Add(hand.Id);

      if (OnHandGraspEvent != null) {
        OnHandGraspEvent(hand);
      }
      if (_graspingIds.Count == 1) {
        OnGraspBegin();
      }
    }

    /// <summary>
    /// Called by InteractionManager every frame that a Hand continues to grasp this object.
    /// </summary>
    public virtual void OnHandsHold(List<Hand> hands) {
      if (OnHandsHoldEvent != null) {
        OnHandsHoldEvent(hands);
      }
    }

    /// <summary>
    /// Called by InteractionManager when a Hand stops grasping this object.
    /// </summary>
    /// <param name="handId"></param>
    public virtual void OnHandRelease(Hand hand) {
      Assert.AreNotEqual(_graspingIds.Count, 0, NoGraspingHandsMessage());
      Assert.IsTrue(_graspingIds.Contains(hand.Id), HandNotGraspingMessage(hand.Id));

      _graspingIds.Remove(hand.Id);

      if (_graspingIds.Count == 0) {
        OnGraspEnd();
      }
    }

    /// <summary>
    /// Called by InteractionManager when a Hand that was grasping becomes untracked.  The Hand
    /// is not yet considered ungrasped, and OnGraspRegainedTracking might be called in the future
    /// if the Hand becomes tracked again.
    /// </summary>
    /// <param name="oldHand"></param>
    public virtual void OnHandLostTracking(Hand oldHand) {
      Assert.AreNotEqual(_graspingIds.Count, 0, NoGraspingHandsMessage());
      Assert.IsTrue(_graspingIds.Contains(oldHand.Id), HandNotGraspingMessage(oldHand.Id));
      Assert.IsFalse(_untrackedIds.Contains(oldHand.Id), HandAlreadyUntrackedMessage(oldHand.Id));

      _untrackedIds.Add(oldHand.Id);
    }

    /// <summary>
    /// Called by InteractionManager when a grasping Hand that had previously been untracked has
    /// regained tracking.  The new hand is provided, as well as the id of the previously tracked
    /// hand.
    /// </summary>
    /// <param name="newHand"></param>
    /// <param name="oldId"></param>
    public virtual void OnHandRegainedTracking(Hand newHand, int oldId) {
      Assert.IsTrue(_graspingIds.Contains(oldId), HandNotGraspingMessage(oldId));
      Assert.IsFalse(_graspingIds.Contains(newHand.Id), HandAlreadyGraspingMessage(newHand.Id));

      _untrackedIds.Remove(oldId);
      _graspingIds.Remove(oldId);
      _graspingIds.Add(newHand.Id);
    }

    /// <summary>
    /// Called by InteractionManager when a untracked grasping Hand has remained ungrasped for
    /// too long.  The hand is no longer considered to be grasping the object.
    /// </summary>
    /// <param name="oldId"></param>
    public virtual void OnHandTimeout(Hand oldHand) {
      _untrackedIds.Remove(oldHand.Id);
      _graspingIds.Remove(oldHand.Id);
    }

    /// <summary>
    /// Calling this method registers this object with the manager.  A shape definition must be registered
    /// with the manager before interaction can be enabled.
    /// </summary>
    public void EnableInteraction() {
      if (_isRegisteredWithManager) {
        return;
      }

      if (_manager == null) {
        throw new NoManagerSpecifiedException();
      }

      _manager.RegisterInteractionBehaviour(this);
      _isRegisteredWithManager = true;
    }

    /// <summary>
    /// Calling this method will unregister this object from the manager.
    /// </summary>
    public void DisableInteraction() {
      if (!_isRegisteredWithManager) {
        return;
      }

      if (_manager == null) {
        throw new NoManagerSpecifiedException();
      }

      _manager.UnregisterInteractionBehaviour(this);
      _isRegisteredWithManager = false;
    }
    #endregion

    #region PROTECTED METHODS
    protected virtual LEAP_IE_SHAPE_DESCRIPTION_HANDLE GenerateShapeDescriptionHandle() {
      return _manager.ShapePool.GetAuto(gameObject);
    }

    protected virtual void OnGraspBegin() {
      if (OnGraspBeginEvent != null) {
        OnGraspBeginEvent();
      }
    }

    protected virtual void OnGraspEnd() {
      if (OnGraspEndEvent != null) {
        OnGraspEndEvent();
      }
    }
    #endregion

    #region ASSERTION MESSAGES
    private static string NoGraspingHandsMessage() {
      return "There are no hands currently grasping this InteractionBehaviour.";
    }

    private static string HandAlreadyGraspingMessage(int id) {
      return "There is already a hand of id " + id + " grapsping this InteractionBehaviour.";
    }

    private static string HandAlreadyUntrackedMessage(int id) {
      return "There is already an untracked hand of id " + id + " grasping this InteractionBehaviour.";
    }

    private static string HandNotGraspingMessage(int id) {
      return "There is not a hand with id " + id + " currently grasping this InteractionBehaviour.";
    }
    #endregion
  }
}
