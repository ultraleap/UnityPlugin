using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  [DisallowMultipleComponent]
  public abstract class InteractionBehaviourBase : IInteractionBehaviour {

    #region SERIALIZED FIELDS
    [SerializeField]
    protected InteractionManager _manager;
    #endregion

    #region INTERNAL FIELDS
    private bool _isRegisteredWithManager = false;

    private bool _hasShapeDescriptionBeenCreated = false;
    private INTERACTION_SHAPE_DESCRIPTION_HANDLE _shapeDescriptionHandle;

    private bool _hasShapeInstanceHandle = false;
    private INTERACTION_SHAPE_INSTANCE_HANDLE _shapeInstanceHandle;

    private List<int> _graspingIds = new List<int>();
    private List<int> _untrackedIds = new List<int>();

    private BaseCallGuard _baseCallGuard = new BaseCallGuard();
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

    public override INTERACTION_SHAPE_DESCRIPTION_HANDLE ShapeDescriptionHandle {
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

      _baseCallGuard.Begin();
      OnRegistered();
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyUnregistered() {
      _isRegisteredWithManager = false;

      _baseCallGuard.Begin();
      OnUnregistered();
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyPreSolve() {
      _baseCallGuard.Begin();
      OnPreSolve();
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyPostSolve() {
      _baseCallGuard.Begin();
      OnPostSolve();
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyInteractionShapeCreated(INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle) {
      _shapeInstanceHandle = instanceHandle;
      _hasShapeInstanceHandle = true;

      _baseCallGuard.Begin();
      OnInteractionShapeCreated(instanceHandle);
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyInteractionShapeDestroyed() {
      _shapeInstanceHandle = new INTERACTION_SHAPE_INSTANCE_HANDLE();
      _shapeDescriptionHandle = new INTERACTION_SHAPE_DESCRIPTION_HANDLE();
      _hasShapeDescriptionBeenCreated = false;
      _hasShapeInstanceHandle = false;

      _baseCallGuard.Begin();
      OnInteractionShapeDestroyed();
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyHandGrasped(Hand hand) {
      Assert.IsFalse(_graspingIds.Contains(hand.Id), HandAlreadyGraspingMessage(hand.Id));

      _graspingIds.Add(hand.Id);

      _baseCallGuard.Begin();
      OnHandGrasped(hand);
      _baseCallGuard.AssertBaseCalled();

      if (_graspingIds.Count == 1) {
        OnGraspBegin();
      }
    }

    public override sealed void NotifyHandsHoldPhysics(List<Hand> hands) {
      _baseCallGuard.Begin();
      OnHandsHoldPhysics(hands);
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyHandsHoldGraphics(List<Hand> hands) {
      _baseCallGuard.Begin();
      OnHandsHoldGraphics(hands);
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyHandReleased(Hand hand) {
      Assert.AreNotEqual(_graspingIds.Count, 0, NoGraspingHandsMessage());
      Assert.IsTrue(_graspingIds.Contains(hand.Id), HandNotGraspingMessage(hand.Id));

      _graspingIds.Remove(hand.Id);

      _baseCallGuard.Begin();
      OnHandReleased(hand);
      _baseCallGuard.AssertBaseCalled();

      if (_graspingIds.Count == 0) {
        OnGraspEnd();
      }
    }

    public override sealed void NotifyHandLostTracking(Hand oldHand) {
      Assert.AreNotEqual(_graspingIds.Count, 0, NoGraspingHandsMessage());
      Assert.IsTrue(_graspingIds.Contains(oldHand.Id), HandNotGraspingMessage(oldHand.Id));
      Assert.IsFalse(_untrackedIds.Contains(oldHand.Id), HandAlreadyUntrackedMessage(oldHand.Id));

      _untrackedIds.Add(oldHand.Id);

      _baseCallGuard.Begin();
      OnHandLostTracking(oldHand);
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyHandRegainedTracking(Hand newHand, int oldId) {
      Assert.IsTrue(_graspingIds.Contains(oldId), HandNotGraspingMessage(oldId));
      Assert.IsFalse(_graspingIds.Contains(newHand.Id), HandAlreadyGraspingMessage(newHand.Id));

      _untrackedIds.Remove(oldId);
      _graspingIds.Remove(oldId);
      _graspingIds.Add(newHand.Id);

      _baseCallGuard.Begin();
      OnHandRegainedTracking(newHand, oldId);
      _baseCallGuard.AssertBaseCalled();
    }

    public override sealed void NotifyHandTimeout(Hand oldHand) {
      _untrackedIds.Remove(oldHand.Id);
      _graspingIds.Remove(oldHand.Id);

      _baseCallGuard.Begin();
      OnHandTimeout(oldHand);
      _baseCallGuard.AssertBaseCalled();

      //OnGraspEnd is dispatched in OnHandTimeout in addition to OnHandRelease
      if (_graspingIds.Count == 0) {
        OnGraspEnd();
      }
    }

    public override sealed void NotifyRecievedSimulationResults(INTERACTION_SHAPE_INSTANCE_RESULTS results) { }
    #endregion

    #region PROTECTED METHODS

    protected virtual void OnRegistered() { }

    protected virtual void OnUnregistered() { }

    protected virtual void OnPreSolve() { }

    protected virtual void OnPostSolve() { }

    protected virtual void OnInteractionShapeCreated(INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle) { }

    protected virtual void OnInteractionShapeDestroyed() { }

    protected virtual void OnRecievedSimulationResults(INTERACTION_SHAPE_INSTANCE_RESULTS results) { }

    protected virtual void OnHandGrasped(Hand hand) { }

    protected virtual void OnHandsHoldPhysics(List<Hand> hands) { }

    protected virtual void OnHandsHoldGraphics(List<Hand> hands) { }

    protected virtual void OnHandReleased(Hand hand) { }

    protected virtual void OnHandLostTracking(Hand oldHand) { }

    protected virtual void OnHandRegainedTracking(Hand newHand, int oldId) { }

    protected virtual void OnHandTimeout(Hand oldHand) { }

    /// <summary>
    /// This method is called to generate the internal description of the interaction shape.
    /// The default implementation uses the GetAuto() method of the ShapeDescriptionPool class, which
    /// accounts for all colliders on this gameObject and its children.
    /// </summary>
    /// <returns></returns>
    protected virtual INTERACTION_SHAPE_DESCRIPTION_HANDLE GenerateShapeDescriptionHandle() {
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
    protected virtual void Reset() {
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
