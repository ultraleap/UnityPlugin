using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  [DisallowMultipleComponent]
  public abstract class InteractionBehaviourBase : MonoBehaviour, IInteractionBehaviour {

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
    #endregion

    #region PUBLIC METHODS

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

    public virtual void EnableInteraction() {
      if (_isRegisteredWithManager) {
        return;
      }

      if (_manager == null) {
        throw new NoManagerSpecifiedException();
      }

      _manager.RegisterInteractionBehaviour(this);
    }

    public virtual void DisableInteraction() {
      if (!_isRegisteredWithManager) {
        return;
      }

      if (_manager == null) {
        throw new NoManagerSpecifiedException();
      }

      _manager.UnregisterInteractionBehaviour(this);
    }

    public INTERACTION_SHAPE_DESCRIPTION_HANDLE ShapeDescriptionHandle {
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

    public INTERACTION_SHAPE_INSTANCE_HANDLE ShapeInstanceHandle {
      get {
        if (!_hasShapeInstanceHandle) {
          throw new InvalidOperationException("Cannot get ShapeInstanceHandle because it has not been assigned.");
        }

        return _shapeInstanceHandle;
      }
    }

    public bool IsBeingGrasped {
      get {
        return _graspingIds.Count > 0;
      }
    }

    public int GraspingHandCount {
      get {
        return _graspingIds.Count;
      }
    }

    public int UntrackedHandCount {
      get {
        return _untrackedIds.Count;
      }
    }

    public IEnumerable<int> GraspingHands {
      get {
        return _graspingIds;
      }
    }

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

    public IEnumerable<int> UntrackedGraspingHands {
      get {
        return _untrackedIds;
      }
    }

    public bool IsBeingGraspedByHand(int handId) {
      return _graspingIds.Contains(handId);
    }
    #endregion

    #region MANAGER CALLBACKS

    public virtual void OnRegister() {
      _isRegisteredWithManager = true;
    }

    public virtual void OnUnregister() {
      _isRegisteredWithManager = false;
    }

    public virtual void OnPreSolve() { }

    public virtual void OnPostSolve() { }

    public abstract void OnInteractionShapeCreationInfo(out INTERACTION_CREATE_SHAPE_INFO createInfo, out INTERACTION_TRANSFORM createTransform);

    public virtual void OnInteractionShapeCreated(INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle) {
      _shapeInstanceHandle = instanceHandle;
      _hasShapeInstanceHandle = true;
    }

    public abstract void OnInteractionShapeUpdate(out INTERACTION_UPDATE_SHAPE_INFO updateInfo, out INTERACTION_TRANSFORM interactionTrasnform);

    public virtual void OnInteractionShapeDestroyed() {
      _shapeInstanceHandle = new INTERACTION_SHAPE_INSTANCE_HANDLE();
      _shapeDescriptionHandle = new INTERACTION_SHAPE_DESCRIPTION_HANDLE();
      _hasShapeDescriptionBeenCreated = false;
      _hasShapeInstanceHandle = false;
    }

    public virtual void OnHandGrasp(Hand hand) {
      Assert.IsFalse(_graspingIds.Contains(hand.Id), HandAlreadyGraspingMessage(hand.Id));

      _graspingIds.Add(hand.Id);

      if (_graspingIds.Count == 1) {
        OnGraspBegin();
      }
    }

    public virtual void OnHandsHold(List<Hand> hands) { }

    public virtual void OnHandRelease(Hand hand) {
      Assert.AreNotEqual(_graspingIds.Count, 0, NoGraspingHandsMessage());
      Assert.IsTrue(_graspingIds.Contains(hand.Id), HandNotGraspingMessage(hand.Id));

      _graspingIds.Remove(hand.Id);

      if (_graspingIds.Count == 0) {
        OnGraspEnd();
      }
    }

    public virtual void OnHandLostTracking(Hand oldHand) {
      Assert.AreNotEqual(_graspingIds.Count, 0, NoGraspingHandsMessage());
      Assert.IsTrue(_graspingIds.Contains(oldHand.Id), HandNotGraspingMessage(oldHand.Id));
      Assert.IsFalse(_untrackedIds.Contains(oldHand.Id), HandAlreadyUntrackedMessage(oldHand.Id));

      _untrackedIds.Add(oldHand.Id);
    }

    public virtual void OnHandRegainedTracking(Hand newHand, int oldId) {
      Assert.IsTrue(_graspingIds.Contains(oldId), HandNotGraspingMessage(oldId));
      Assert.IsFalse(_graspingIds.Contains(newHand.Id), HandAlreadyGraspingMessage(newHand.Id));

      _untrackedIds.Remove(oldId);
      _graspingIds.Remove(oldId);
      _graspingIds.Add(newHand.Id);
    }

    public virtual void OnHandTimeout(Hand oldHand) {
      _untrackedIds.Remove(oldHand.Id);
      _graspingIds.Remove(oldHand.Id);

      //OnGraspEnd is dispatched in OnHandTimeout in addition to OnHandRelease
      if (_graspingIds.Count == 0) {
        OnGraspEnd();
      }
    }

    public virtual void OnRecieveSimulationResults(INTERACTION_SHAPE_INSTANCE_RESULTS results) { }
    #endregion

    #region PROTECTED METHODS
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
      EnableInteraction();
    }

    protected virtual void OnDisable() {
      DisableInteraction();
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
