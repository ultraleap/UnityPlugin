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

    #region INTERNAL FIELDS
    private bool _isRegisteredWithManager = false;

    private bool _hasShapeDescriptionBeenCreated = false;
    private LEAP_IE_SHAPE_DESCRIPTION_HANDLE _shapeDescriptionHandle;
    private LEAP_IE_SHAPE_INSTANCE_HANDLE _shapeInstanceHandle;

    protected Rigidbody _rigidbody;
    bool _rigidbodyHadUseGravity = false;

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
    /// Gets the handle to the internal shape description.
    /// </summary>
    public LEAP_IE_SHAPE_DESCRIPTION_HANDLE ShapeDescriptionHandle {
      get {
        if (!_isRegisteredWithManager) {
          throw new NotRegisteredWithManagerException();
        }

        if (!_hasShapeDescriptionBeenCreated) {
          _shapeDescriptionHandle = GenerateShapeDescriptionHandle();
          _hasShapeDescriptionBeenCreated = true;
        }
        return _shapeDescriptionHandle;
      }
    }

    /// <summary>
    /// Gets the handle to the internal shape instance.
    /// </summary>
    public LEAP_IE_SHAPE_INSTANCE_HANDLE ShapeInstanceHandle {
      get {
        if (!_isRegisteredWithManager) {
          throw new NotRegisteredWithManagerException();
        }

        return _shapeInstanceHandle;
      }
    }

    /// <summary>
    /// Gets the internal representation of the transform of the object.
    /// </summary>
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
      throw new Exception("is it bad?");
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
    /// Called by InteractionManager when a Hand begins grasping this object.
    /// </summary>
    /// <param name="handId"></param>
    public virtual void OnHandGrasp(Hand hand) {
      Assert.IsFalse(_graspingIds.Contains(hand.Id), HandAlreadyGraspingMessage(hand.Id));

      _graspingIds.Add(hand.Id);

      if (_graspingIds.Count == 1) {
        OnGraspBegin();
      }
    }

    /// <summary>
    /// Called by InteractionManager every frame that a Hand continues to grasp this object.
    /// </summary>
    public virtual void OnHandsHold(List<Hand> hands) { }

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
    /// Called by InteractionManager when an untracked grasping Hand has remained ungrasped for
    /// too long.  The hand is no longer considered to be grasping the object.
    /// </summary>
    /// <param name="oldId"></param>
    public virtual void OnHandTimeout(Hand oldHand) {
      _untrackedIds.Remove(oldHand.Id);
      _graspingIds.Remove(oldHand.Id);

      //OnGraspEnd is dispatched in OnHandTimeout in addition to OnHandRelease
      if (_graspingIds.Count == 0) {
        OnGraspEnd();
      }
    }

    /// <summary>
    /// Called by InteractionController when the velocity of an object is changed.
    /// </summary>
    public virtual void OnVelocityChanged(UnityEngine.Vector3 linearVelocity, UnityEngine.Vector3 angularVelocity) {
      if (!_rigidbody)
        return;

      if (_rigidbody.useGravity)
        throw new InvalidOperationException("Cannot modify velocity of object correctly because it has a force applied (gravity.)");

      // Clear applied forces.  They were not accounted for when the velocities were calculated.
      _rigidbody.Sleep();

      _rigidbody.velocity = linearVelocity;
      _rigidbody.angularVelocity = angularVelocity;
    }

    public virtual void OnRegister() {
      _isRegisteredWithManager = true;

      _rigidbody = GetComponent<Rigidbody>();
      if (_rigidbody) {
        _rigidbodyHadUseGravity = _rigidbody.useGravity;
        _rigidbody.useGravity = false;
      }
    }

    public virtual void OnUnregister() {
      _isRegisteredWithManager = false;

      if (_rigidbody) {
        _rigidbody.useGravity = _rigidbodyHadUseGravity;
        _rigidbody = null;
      }
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
    }
    #endregion

    #region PROTECTED METHODS
    /// <summary>
    /// This method is called to generate the internal description of the interaction shape.
    /// The default implementation uses the GetAuto() method of the ShapeDescriptionPool class, which
    /// accounts for all colliders on this gameObject and its children.
    /// </summary>
    /// <returns></returns>
    protected virtual LEAP_IE_SHAPE_DESCRIPTION_HANDLE GenerateShapeDescriptionHandle() {
      return _manager.ShapePool.GetAuto(gameObject);
    }

    /// <summary>
    /// Called when the the object transitions from being grasped by no hands to being
    /// grasped by at least one hand.
    /// </summary>
    protected virtual void OnGraspBegin() { }

    /// <summary>
    /// Called when the object transitions from being grasped by at least one hand
    /// to being grasped by no hands.
    /// </summary>
    protected virtual void OnGraspEnd() { }
    #endregion

    #region UNITY MESSAGES
    protected virtual void OnValidate() {
      checkForParentBehaviour();
    }

    protected virtual void Reset() {
      if (_manager == null) {
        //If manager is null, first check out parents for one, then search the whole scene
        _manager = GetComponentInParent<InteractionManager>();
        if (_manager == null) {
          _manager = FindObjectOfType<InteractionManager>();
        }
      }

      checkForParentBehaviour();
    }

    protected virtual void OnEnable() {
      EnableInteraction();
    }

    protected virtual void OnDisable() {
      DisableInteraction();
    }

    private void checkForParentBehaviour() {
      if (GetComponentsInParent<InteractionBehaviour>().Length > 1) {
        Debug.LogError("InteractionBehaviour cannot be a child OR sibling of another InteractionBehaviour!");
#if UNITY_EDITOR
        UnityEditor.Selection.activeGameObject = gameObject;
#endif
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
