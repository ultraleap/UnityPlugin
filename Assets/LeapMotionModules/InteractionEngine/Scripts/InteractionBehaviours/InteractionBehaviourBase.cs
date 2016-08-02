using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  /// <summary>
  /// InteractionBehaviourBase is a utility class that implements the IInteractionBehaviour interface.  This class
  /// aims to provide the minimum set of behaviours that can be generalized to most interaction objects, and no more.
  /// The aim is to provide a class that takes care of the 'bookkeeping' needed to properly interface with the
  /// InteractionManager component, so that implementing different types of InteractionBehaviours becomes easier.
  /// </summary>
  ///
  /// <remarks>
  /// This behaviour has the following features:
  ///    - Takes care of most of the bookkeeping needed to interface with the InteractionManager
  ///    - Contains protected callbacks for use of an implementing class.  Certain callbacks like OnGraspBegin and
  ///      OnGraspEnd provide utility functionality that is not provided by InteractionManager.
  ///
  /// This behaviour has the following requirements:
  ///    - This behaviour cannot be a sibling or a child of any other InteractionBehaviourBase.
  ///    - Certain callbacks have been left unimplemented and must be implemented by an extending class.
  ///    - Only a single collider is supported currently.  The collider must be either a BoxCollider or SphereCollider,
  ///      and must not be on a child transform.  The collider must also have a center value of zero.
  /// </remarks>
  [DisallowMultipleComponent]
  public abstract partial class InteractionBehaviourBase : IInteractionBehaviour {

    #region SERIALIZED FIELDS
    [SerializeField]
    protected InteractionManager _manager;
    #endregion

    #region EVENTS
    public event Action<Hand> OnHandGraspedEvent;
    public event Action<Hand> OnHandReleasedEvent;
    public event Action OnGraspBeginEvent;
    public event Action OnGraspEndEvent;
    #endregion

    #region INTERNAL FIELDS
    private bool _isRegisteredWithManager = false;

    private bool _hasShapeDescriptionBeenCreated = false;
    private INTERACTION_SHAPE_DESCRIPTION_HANDLE _shapeDescriptionHandle;

    private bool _hasShapeInstanceHandle = false;
    private INTERACTION_SHAPE_INSTANCE_HANDLE _shapeInstanceHandle;

    private List<int> _graspingIds = new List<int>();
    private List<int> _untrackedIds = new List<int>();

#if UNITY_ASSERTIONS
    private BaseCallGuard _baseCallGuard = new BaseCallGuard();
#else
    protected BaseCallGuard _baseCallGuard = null;
#endif

    #endregion

    #region PUBLIC METHODS

    public override InteractionManager Manager {
      get {
        return _manager;
      }
      set {
        if (_manager != value) {
          if (_isRegisteredWithManager) {
            enabled = false;
            _manager = value;
            enabled = true;
          } else {
            _manager = value;
          }
        }
      }
    }

    public override bool IsRegisteredWithManager {
      get {
        return _isRegisteredWithManager;
      }
    }

    public bool HasShapeInstance {
      get {
        return _hasShapeInstanceHandle;
      }
    }

    public override INTERACTION_SHAPE_DESCRIPTION_HANDLE ShapeDescriptionHandle {
      get {
        if (!_isRegisteredWithManager) {
          throw new InvalidOperationException("Cannot get Shape Description Handle because the Interaction Object has not yet been registered with an Interaction Manager.");
        }

        if (!_hasShapeDescriptionBeenCreated) {
          _shapeDescriptionHandle = GenerateShapeDescriptionHandle();
          _hasShapeDescriptionBeenCreated = true;
        }
        return _shapeDescriptionHandle;
      }
    }

    public override INTERACTION_SHAPE_INSTANCE_HANDLE ShapeInstanceHandle {
      get {
        if (!_hasShapeInstanceHandle) {
          throw new InvalidOperationException("Cannot get ShapeInstanceHandle because it has not been assigned.");
        }

        return _shapeInstanceHandle;
      }
    }

    public override bool IsBeingGrasped {
      get {
        return _graspingIds.Count > 0;
      }
    }

    public override int GraspingHandCount {
      get {
        return _graspingIds.Count;
      }
    }

    public override int UntrackedHandCount {
      get {
        return _untrackedIds.Count;
      }
    }

    public override IEnumerable<int> GraspingHands {
      get {
        return _graspingIds;
      }
    }

    public override IEnumerable<int> TrackedGraspingHands {
      get {
        for (int i = 0; i < _graspingIds.Count; i++) {
          int id = _graspingIds[i];
          if (!_untrackedIds.Contains(id)) {
            yield return id;
          }
        }
      }
    }

    public override IEnumerable<int> UntrackedGraspingHands {
      get {
        return _untrackedIds;
      }
    }

    public override bool IsBeingGraspedByHand(int handId) {
      return _graspingIds.Contains(handId);
    }
    #endregion

    #region MANAGER CALLBACKS

    public override sealed void NotifyRegistered() {
      _isRegisteredWithManager = true;

      _baseCallGuard.Begin("OnRegistered");
      OnRegistered();
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyUnregistered() {
      _isRegisteredWithManager = false;
      if (isActiveAndEnabled) {
        enabled = false;
      }

      if (_hasShapeDescriptionBeenCreated) {
        _manager.ShapePool.ReturnShape(_shapeDescriptionHandle);
        _shapeDescriptionHandle = new INTERACTION_SHAPE_DESCRIPTION_HANDLE();
        _hasShapeDescriptionBeenCreated = false;
      }

      _baseCallGuard.Begin("OnUnregistered");
      OnUnregistered();
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyPreSolve() {
      _baseCallGuard.Begin("OnPreSolve");
      OnPreSolve();
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyPostSolve() {
      _baseCallGuard.Begin("OnPostSolve");
      OnPostSolve();
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyInteractionShapeCreated(INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle) {
      _shapeInstanceHandle = instanceHandle;
      _hasShapeInstanceHandle = true;

      _baseCallGuard.Begin("OnInteractionShapeCreated");
      OnInteractionShapeCreated(instanceHandle);
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyInteractionShapeDestroyed() {
      _shapeInstanceHandle = new INTERACTION_SHAPE_INSTANCE_HANDLE();
      _hasShapeInstanceHandle = false;

      _baseCallGuard.Begin("OnInteractionShapeDestroyed");
      OnInteractionShapeDestroyed();
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyHandGrasped(Hand hand) {
      Assert.IsFalse(_graspingIds.Contains(hand.Id), HandAlreadyGraspingMessage(hand.Id));

      _graspingIds.Add(hand.Id);

      _baseCallGuard.Begin("OnHandGrasped");
      OnHandGrasped(hand);
      _baseCallGuard.AssertBaseCalled();

      if (_graspingIds.Count == 1) {
        _baseCallGuard.Begin("OnGraspBegin");
        OnGraspBegin();
        _baseCallGuard.AssertBaseCalled();
      }
    }

    public override sealed void NotifyHandsHoldPhysics(ReadonlyList<Hand> hands) {
      _baseCallGuard.Begin("OnHandsHoldPhysics");
      OnHandsHoldPhysics(hands);
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyHandsHoldGraphics(ReadonlyList<Hand> hands) {
      _baseCallGuard.Begin("OnHandsHoldGraphics");
      OnHandsHoldGraphics(hands);
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyHandReleased(Hand hand) {
      Assert.AreNotEqual(_graspingIds.Count, 0, NoGraspingHandsMessage());
      Assert.IsTrue(_graspingIds.Contains(hand.Id), HandNotGraspingMessage(hand.Id));

      _graspingIds.Remove(hand.Id);

      _baseCallGuard.Begin("OnHandReleased");
      OnHandReleased(hand);
      _baseCallGuard.AssertBaseCalled();

      if (_graspingIds.Count == 0) {
        _baseCallGuard.Begin("OnGraspEnd");
        OnGraspEnd(hand);
        _baseCallGuard.AssertBaseCalled();
      }
    }

    public override sealed void NotifyHandLostTracking(Hand oldHand, out float maxSuspensionTime) {
      Assert.AreNotEqual(_graspingIds.Count, 0, NoGraspingHandsMessage());
      Assert.IsTrue(_graspingIds.Contains(oldHand.Id), HandNotGraspingMessage(oldHand.Id));
      Assert.IsFalse(_untrackedIds.Contains(oldHand.Id), HandAlreadyUntrackedMessage(oldHand.Id));

      _untrackedIds.Add(oldHand.Id);

      _baseCallGuard.Begin("OnHandLostTracking");
      OnHandLostTracking(oldHand, out maxSuspensionTime);
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyHandRegainedTracking(Hand newHand, int oldId) {
      Assert.IsTrue(_graspingIds.Contains(oldId), HandNotGraspingMessage(oldId));
      Assert.IsFalse(_graspingIds.Contains(newHand.Id), HandAlreadyGraspingMessage(newHand.Id));

      _untrackedIds.Remove(oldId);
      _graspingIds.Remove(oldId);
      _graspingIds.Add(newHand.Id);

      _baseCallGuard.Begin("OnHandRegainedTracking");
      OnHandRegainedTracking(newHand, oldId);
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyHandTimeout(Hand oldHand) {
      _untrackedIds.Remove(oldHand.Id);
      _graspingIds.Remove(oldHand.Id);

      _baseCallGuard.Begin("OnHandTimeout");
      OnHandTimeout(oldHand);
      _baseCallGuard.AssertBaseCalled();

      // OnGraspEnd is dispatched in OnHandTimeout in addition to OnHandRelease
      if (_graspingIds.Count == 0) {
        _baseCallGuard.Begin("OnGraspEnd");
        OnGraspEnd(null);
        _baseCallGuard.AssertBaseCalled();
      }
    }

    public override sealed void NotifyRecievedSimulationResults(INTERACTION_SHAPE_INSTANCE_RESULTS results) {
      _baseCallGuard.Begin("OnRecievedSimulationResults");
      OnRecievedSimulationResults(results);
      _baseCallGuard.AssertBaseCalled();
    }
    #endregion

    #region PROTECTED METHODS

    /// <summary>
    /// This method is called to generate the internal description of the interaction shape.
    /// The default implementation uses the GetCollision() method of the ShapeDescriptionPool class, which
    /// accounts for all colliders on this gameObject and its children.
    /// </summary>
    /// <returns></returns>
    protected virtual INTERACTION_SHAPE_DESCRIPTION_HANDLE GenerateShapeDescriptionHandle() {
      return _manager.ShapePool.GetCollision(gameObject);
    }

    /// <summary>
    /// Called when the behaviour is successfully registered with the manager.
    /// </summary>
    protected virtual void OnRegistered() {
      _baseCallGuard.NotifyBaseCalled("OnRegistered");
    }

    /// <summary>
    /// Called when the behaviour is unregistered with the manager.
    /// </summary>
    protected virtual void OnUnregistered() {
      _baseCallGuard.NotifyBaseCalled("OnUnregistered");
    }

    /// <summary>
    /// Called before any solving is performed.
    /// </summary>
    protected virtual void OnPreSolve() {
      _baseCallGuard.NotifyBaseCalled("OnPreSolve");
    }

    /// <summary>
    /// Called after all solving is performed.
    /// </summary>
    protected virtual void OnPostSolve() {
      _baseCallGuard.NotifyBaseCalled("OnPostSolve");
    }

    /// <summary>
    /// Called when the interaction shape associated with this InteractionBehaviour
    /// is created and added to the interaction scene.
    /// </summary>
    protected virtual void OnInteractionShapeCreated(INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle) {
      _baseCallGuard.NotifyBaseCalled("OnInteractionShapeCreated");
    }

    /// <summary>
    /// Called when the interaction shape associated with this InteractionBehaviour
    /// is destroyed and removed from the interaction scene.
    /// </summary>
    protected virtual void OnInteractionShapeDestroyed() {
      _baseCallGuard.NotifyBaseCalled("OnInteractionShapeDestroyed");
    }

    /// <summary>
    /// Called when simulation results are available for this object.
    /// </summary>
    protected virtual void OnRecievedSimulationResults(INTERACTION_SHAPE_INSTANCE_RESULTS results) {
      _baseCallGuard.NotifyBaseCalled("OnRecievedSimulationResults");
    }

    /// <summary>
    /// Called when a Hand begins grasping this object.
    /// </summary>
    protected virtual void OnHandGrasped(Hand hand) {
      _baseCallGuard.NotifyBaseCalled("OnHandGrasped");
      if (OnHandGraspedEvent != null) {
        OnHandGraspedEvent(hand);
      }
    }

    /// <summary>
    /// Called every FixedUpdate that a Hand continues to grasp this object.
    /// </summary>
    protected virtual void OnHandsHoldPhysics(ReadonlyList<Hand> hands) {
      _baseCallGuard.NotifyBaseCalled("OnHandsHoldPhysics");
    }

    /// <summary>
    /// Called every LateUpdate that a Hand continues to grasp this object.
    /// </summary>
    protected virtual void OnHandsHoldGraphics(ReadonlyList<Hand> hands) {
      _baseCallGuard.NotifyBaseCalled("OnHandsHoldGraphics");
    }

    /// <summary>
    /// Called when a Hand stops grasping this object.
    /// </summary>
    protected virtual void OnHandReleased(Hand hand) {
      _baseCallGuard.NotifyBaseCalled("OnHandReleased");
      if (OnHandReleasedEvent != null) {
        OnHandReleasedEvent(hand);
      }
    }

    /// <summary>
    /// Called when a Hand that was grasping becomes untracked.  The Hand
    /// is not yet considered ungrasped, and OnHandRegainedTracking might be called in the future
    /// if the Hand becomes tracked again.
    /// </summary>
    protected virtual void OnHandLostTracking(Hand oldHand, out float maxSuspensionTime) {
      maxSuspensionTime = 0;
      _baseCallGuard.NotifyBaseCalled("OnHandLostTracking");
    }

    /// <summary>
    /// Called when a grasping Hand that had previously been untracked has
    /// regained tracking.  The new hand is provided, as well as the id of the previously tracked
    /// hand.
    /// </summary>
    protected virtual void OnHandRegainedTracking(Hand newHand, int oldId) {
      _baseCallGuard.NotifyBaseCalled("OnHandRegainedTracking");
    }

    /// <summary>
    /// Called when an untracked grasping Hand has remained ungrasped for
    /// too long.  The hand is no longer considered to be grasping the object.
    /// </summary>
    protected virtual void OnHandTimeout(Hand oldHand) {
      _baseCallGuard.NotifyBaseCalled("OnHandTimeout");
    }

    /// <summary>
    /// Called when the the object transitions from being grasped by no hands to being
    /// grasped by at least one hand.
    /// </summary>
    protected virtual void OnGraspBegin() {
      _baseCallGuard.NotifyBaseCalled("OnGraspBegin");
      if (OnGraspBeginEvent != null) {
        OnGraspBeginEvent();
      }
    }

    /// <summary>
    /// Called when the object transitions from being grasped by at least one hand
    /// to being grasped by no hands.
    /// </summary>
    protected virtual void OnGraspEnd(Hand lastHand) {
      _baseCallGuard.NotifyBaseCalled("OnGraspEnd");
      if (OnGraspEndEvent != null) {
        OnGraspEndEvent();
      }
    }
    #endregion

    #region UNITY MESSAGES

    protected virtual void Awake() {
      FindInteractionManager();
    }

    protected virtual void Reset() {
      FindInteractionManager();
    }

    private void FindInteractionManager() {
      if (_manager == null) {
        //If manager is null, first check our parents for one, then search the whole scene
        _manager = GetComponentInParent<InteractionManager>();
        if (_manager == null) {
          _manager = FindObjectOfType<InteractionManager>();
        }
      }
    }

    protected virtual void OnEnable() {
      if (_manager == null) {
        enabled = false;
        Debug.LogError("Could not enable Interaction Behaviour because no Interaction Manager was specified.");
        return;
      }

      if (_isRegisteredWithManager) {
        return;
      }

      _manager.RegisterInteractionBehaviour(this);
    }

    protected virtual void OnDisable() {
      if (_manager == null) {
        return;
      }

      if (!_isRegisteredWithManager) {
        return;
      }

      _manager.UnregisterInteractionBehaviour(this);
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
