using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;
using LeapInternal;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  public class InteractionManager : MonoBehaviour {

    #region SERIALIZED FIELDS
    [SerializeField]
    protected LeapProvider _leapProvider;

    [Tooltip("The amount of time a Hand can remain untracked while also still grasping an object.")]
    [SerializeField]
    protected float _untrackedTimeout = 0.5f;

    [Tooltip("Shows the debug output coming from the internal Interaction plugin.")]
    [SerializeField]
    protected bool _showDebugLines = true;

    [Tooltip("Allow the Interaction plugin to modify object velocities when pushing.")]
    [SerializeField]
    protected bool _modifyVelocities = true;
    #endregion

    #region INTERNAL FIELDS
    protected List<InteractionBehaviourBase> _registeredBehaviours;
    protected Dictionary<LEAP_IE_SHAPE_INSTANCE_HANDLE, InteractionBehaviourBase> _instanceHandleToBehaviour;

    protected Dictionary<int, InteractionHand> _idToInteractionHand;
    protected List<InteractionBehaviourBase> _graspedBehaviours;

    protected ShapeDescriptionPool _shapeDescriptionPool;

    protected LEAP_IE_SCENE _scene;

    //A temp list that is recycled.  Used to remove items from _handIdToIeHand.
    private List<int> _handIdsToRemove;
    //A temp list that is recycled.  Used as the argument to OnHandsHold.
    private List<Hand> _holdingHands;
    #endregion

    #region PUBLIC METHODS
    /// <summary>
    /// Gets the current debug flags for this manager.
    /// </summary>
    public eLeapIEDebugFlags DebugFlags {
      get {
        eLeapIEDebugFlags flags = eLeapIEDebugFlags.eLeapIEDebugFlags_None;
        if (_showDebugLines) {
          flags |= eLeapIEDebugFlags.eLeapIEDebugFlags_Lines;
        }
        return flags;
      }
    }

    /// <summary>
    /// Returns a ShapeDescriptionPool that can be used to allocate shape descriptions
    /// for this manager.  Using the pool can be more efficient since identical shapes
    /// can be automatically combined to save memory.  Shape descriptions aquired from this
    /// pool will be destroyed when this manager is disabled.
    /// </summary>
    public ShapeDescriptionPool ShapePool {
      get {
        return _shapeDescriptionPool;
      }
    }

    /// <summary>
    /// Returns true if any InteractionObject is currently being grasped by at least one Hand.
    /// </summary>
    public bool IsAnyObjectGrasped {
      get {
        return _graspedBehaviours.Count != 0;
      }
    }

    /// <summary>
    /// Returns a collection of InteractionObjects that are currently being grasped by
    /// at least one hand.
    /// </summary>
    public IEnumerable<InteractionBehaviourBase> GraspedObjects {
      get {
        return _graspedBehaviours;
      }
    }

    /// <summary>
    /// Tries to find an InteractionObject that is currently being grasped by a Hand with
    /// the given ID.
    /// </summary>
    /// <param name="handId"></param>
    /// <param name="graspedObject"></param>
    /// <returns></returns>
    public bool TryGetGraspedObject(int handId, out InteractionBehaviourBase graspedObject) {
      for (int i = 0; i < _graspedBehaviours.Count; i++) {
        var iObj = _graspedBehaviours[i];
        if (iObj.IsBeingGraspedByHand(handId)) {
          graspedObject = iObj;
          return true;
        }
      }

      graspedObject = null;
      return false;
    }

    /// <summary>
    /// Registers an InteractionObject with this manager, which automatically adds the objects
    /// representation into the internal interaction scene.  If the manager is disabled,
    /// the registration will still succeed and the object will be added to the internal scene
    /// when the manager is next enabled.
    /// </summary>
    /// <param name="interactionBehaviour"></param>
    public void RegisterInteractionBehaviour(InteractionBehaviourBase interactionBehaviour) {
      _registeredBehaviours.Add(interactionBehaviour);

      //Don't create right away if we are not enabled, creation will be done in OnEnable
      if (isActiveAndEnabled) {
        createInteractionShape(interactionBehaviour);
      }
    }

    /// <summary>
    /// Unregisters an InteractionObject from this manager.  This removes it from the internal
    /// scene and prevents any further interaction.
    /// </summary>
    /// <param name="interactionBehaviour"></param>
    public void UnregisterInteractionBehaviour(InteractionBehaviourBase interactionBehaviour) {
      _registeredBehaviours.Remove(interactionBehaviour);

      //Don't destroy if we are not enabled, everything already got destroyed in OnDisable
      if (isActiveAndEnabled) {
        destroyInteractionShape(interactionBehaviour);
      }
    }
    #endregion

    #region UNITY CALLBACKS
    protected virtual void Reset() {
      if (_leapProvider == null) {
        _leapProvider = FindObjectOfType<LeapProvider>();
      }
    }

    protected virtual void OnValidate() {
      if (Application.isPlaying && isActiveAndEnabled) {
        //Allow the debug lines to be toggled while the scene is playing
        applyDebugSettings();
      }
    }

    protected virtual void Awake() {
      _registeredBehaviours = new List<InteractionBehaviourBase>();
      _instanceHandleToBehaviour = new Dictionary<LEAP_IE_SHAPE_INSTANCE_HANDLE, InteractionBehaviourBase>();
      _graspedBehaviours = new List<InteractionBehaviourBase>();
      _idToInteractionHand = new Dictionary<int, InteractionHand>();
      _handIdsToRemove = new List<int>();
      _holdingHands = new List<Hand>();
    }

    protected virtual void OnEnable() {
      InteractionC.CreateScene(ref _scene);
      _shapeDescriptionPool = new ShapeDescriptionPool(_scene);
      applyDebugSettings();

      Assert.AreEqual(_instanceHandleToBehaviour.Count, 0, "There should not be any instances before the creation step.");

      for (int i = 0; i < _registeredBehaviours.Count; i++) {
        createInteractionShape(_registeredBehaviours[i]);
      }
    }

    protected virtual void OnDisable() {
      foreach (var interactionHand in _idToInteractionHand.Values) {
        if (interactionHand.graspedObject != null) {
          interactionHand.ReleaseObject();
        }
      }
      _idToInteractionHand.Clear();
      _graspedBehaviours.Clear();

      for (int i = 0; i < _registeredBehaviours.Count; i++) {
        destroyInteractionShape(_registeredBehaviours[i]);
      }

      Assert.AreEqual(_instanceHandleToBehaviour.Count, 0, "All instances should have been destroyed.");

      _shapeDescriptionPool.RemoveAllShapes();
      _shapeDescriptionPool = null;
      InteractionC.DestroyScene(ref _scene);
    }

    protected virtual void FixedUpdate() {
      simulateFrame(_leapProvider.CurrentFrame);

      if (_showDebugLines) {
        InteractionC.DrawDebugLines(ref _scene);
      }
    }
    #endregion

    #region INTERNAL METHODS
    protected virtual void simulateFrame(Frame frame) {
      updateInteractionRepresentations();

      updateTracking(frame);

      simulateInteraction();

      updateInteractionStateChanges();

      // TODO: Pass a debug flag to disable calculating velocities.
      if(_modifyVelocities)
        setObjectVelocities();
    }

    protected virtual void applyDebugSettings() {
      InteractionC.EnableDebugFlags(ref _scene, (uint)DebugFlags);
    }

    protected virtual void updateInteractionRepresentations() {
      for (int i = 0; i < _registeredBehaviours.Count; i++) {
        InteractionBehaviourBase interactionBehaviour = _registeredBehaviours[i];
        LEAP_IE_SHAPE_INSTANCE_HANDLE shapeInstanceHandle = interactionBehaviour.ShapeInstanceHandle;
        LEAP_IE_TRANSFORM interactionTransform = interactionBehaviour.InteractionTransform;
        InteractionC.UpdateShape(ref _scene, ref interactionTransform, ref shapeInstanceHandle);
      }
    }

    protected virtual void updateTracking(Frame frame) {
      int handCount = frame.Hands.Count;
      IntPtr ptr = HandArrayBuilder.CreateHandArray(frame);
      InteractionC.UpdateHands(ref _scene, (uint)handCount, ptr);
      StructAllocator.CleanupAllocations();
    }

    protected virtual void simulateInteraction() {
      var _controllerTransform = new LEAP_IE_TRANSFORM();
      _controllerTransform.position = new LEAP_VECTOR(_leapProvider.transform.position);
      _controllerTransform.rotation = new LEAP_QUATERNION(_leapProvider.transform.rotation);
      _controllerTransform.wallTime = Time.fixedTime;

      InteractionC.UpdateController(ref _scene, ref _controllerTransform);
    }

    protected virtual void updateInteractionStateChanges() {
      var hands = _leapProvider.CurrentFrame.Hands;

      //First loop through all the hands and get their classifications from the engine
      for (int i = 0; i < hands.Count; i++) {
        Hand hand = hands[i];

        LEAP_IE_HAND_CLASSIFICATION classification;
        LEAP_IE_SHAPE_INSTANCE_HANDLE instance;
        InteractionC.GetClassification(ref _scene,
                                       (uint)hand.Id,
                                       out classification,
                                       out instance);



        //Get the InteractionHand associated with this hand id
        InteractionHand interactionHand;
        if (!_idToInteractionHand.TryGetValue(hand.Id, out interactionHand)) {

          //First we see if there is an untracked interactionHand that can be re-connected using this one
          InteractionHand untrackedInteractionHand = null;
          foreach (var pair in _idToInteractionHand) {
            //If the old ieHand is untracked, and the handedness matches, we re-connect it
            if (pair.Value.isUntracked && pair.Value.hand.IsLeft == hand.IsLeft) {
              untrackedInteractionHand = pair.Value;
              break;
            }
          }

          if (untrackedInteractionHand != null) {
            //If we found an untrackedIeHand, use it!
            interactionHand = untrackedInteractionHand;
            //Remove the old id from the mapping
            _idToInteractionHand.Remove(untrackedInteractionHand.hand.Id);
            //This also dispatched InteractionObject.OnHandRegainedTracking()
            interactionHand.RegainTracking(hand);
          } else {
            //Otherwise just create a new one
            interactionHand = new InteractionHand(hand);
          }

          //In both cases, associate the id with the new ieHand
          _idToInteractionHand[hand.Id] = interactionHand;
        }

        interactionHand.UpdateHand(hand);

        switch (classification.classification) {
          case eLeapIEClassification.eLeapIEClassification_Grasp:
            {
              InteractionBehaviourBase interactionBehaviour = _instanceHandleToBehaviour[instance];
              if (interactionHand.graspedObject == null) {
                _graspedBehaviours.Add(interactionBehaviour);
                interactionHand.GraspObject(interactionBehaviour);
              }
              break;
            }
          case eLeapIEClassification.eLeapIEClassification_Physics:
            {
              if (interactionHand.graspedObject != null) {
                _graspedBehaviours.Remove(interactionHand.graspedObject);
                interactionHand.ReleaseObject();
              }
              break;
            }
          default:
            throw new InvalidOperationException("Unexpected classification " + classification.classification);
        }
      }

      //Loop through all ieHands to check for timeouts and loss of tracking
      foreach (var pair in _idToInteractionHand) {
        var id = pair.Key;
        var ieHand = pair.Value;

        float handAge = Time.time - ieHand.lastTimeUpdated;
        //Check to see if the hand is at least 1 frame old
        //We assume it has become untracked if this is the case
        if (handAge > 0) {
          //If the hand isn't grasping anything, just remove it
          if (ieHand.graspedObject == null) {
            _handIdsToRemove.Add(id);
            continue;
          }

          //If is isn't already marked as untracked, mark it as untracked
          if (!ieHand.isUntracked) {
            //This also dispatches InteractionObject.OnHandLostTracking()
            ieHand.MarkUntracked();
          }

          //If the age is longer than the timeout, we also remove it from the list
          if (handAge > _untrackedTimeout) {
            _handIdsToRemove.Add(id);
            //This also dispatched InteractionObject.OnHandTimeout()
            ieHand.MarkTimeout();
            continue;
          }
        }
      }

      //Loop through the stale ids and remove them from the map
      for (int i = 0; i < _handIdsToRemove.Count; i++) {
        _idToInteractionHand.Remove(_handIdsToRemove[i]);
      }
      _handIdsToRemove.Clear();

      //Loop through the currently grasped objects to dispatch their OnHandsHold callback
      for (int i = 0; i < _graspedBehaviours.Count; i++) {
        var interactionBehaviour = _graspedBehaviours[i];

        for (int j = 0; j < hands.Count; j++) {
          if (interactionBehaviour.IsBeingGraspedByHand(hands[j].Id)) {
            _holdingHands.Add(hands[j]);
          }
        }

        interactionBehaviour.OnHandsHold(_holdingHands);
        _holdingHands.Clear();
      }
    }

    protected virtual void setObjectVelocities()
    {
      LEAP_IE_VELOCITY[] velocities;
      InteractionC.GetVelocities(ref _scene, out velocities);

      if (velocities == null)
        return;

      for (int i = 0; i < velocities.Length; ++i)
      {
        LEAP_IE_VELOCITY vel = velocities[i];
        _instanceHandleToBehaviour[vel.handle].OnVelocityChanged(vel.linearVelocity.ToUnityVector(), vel.angularVelocity.ToUnityVector());
      }
    }

    protected virtual void createInteractionShape(InteractionBehaviourBase interactionBehaviour) {
      LEAP_IE_SHAPE_DESCRIPTION_HANDLE descriptionHandle = interactionBehaviour.ShapeDescriptionHandle;
      LEAP_IE_SHAPE_INSTANCE_HANDLE instanceHandle = new LEAP_IE_SHAPE_INSTANCE_HANDLE();
      LEAP_IE_TRANSFORM interactionTransform = interactionBehaviour.InteractionTransform;

      InteractionC.CreateShape(ref _scene, ref descriptionHandle, ref interactionTransform, out instanceHandle);

      _instanceHandleToBehaviour[instanceHandle] = interactionBehaviour;

      interactionBehaviour.OnInteractionShapeCreated(instanceHandle);
    }

    protected virtual void destroyInteractionShape(InteractionBehaviourBase interactionBehaviour) {
      LEAP_IE_SHAPE_INSTANCE_HANDLE instanceHandle = interactionBehaviour.ShapeInstanceHandle;

      _instanceHandleToBehaviour.Remove(instanceHandle);

      InteractionC.DestroyShape(ref _scene, ref instanceHandle);

      interactionBehaviour.OnInteractionShapeDestroyed();
    }
    #endregion

    #region INTERNAL CLASSES
    //A persistant structure for storing useful data about a hand as it interacts with objects
    protected class InteractionHand {
      public Hand hand { get; protected set; }
      public float lastTimeUpdated { get; protected set; }
      public InteractionBehaviourBase graspedObject { get; protected set; }
      public bool isUntracked { get; protected set; }

      public InteractionHand(Hand hand) {
        this.hand = hand;
        lastTimeUpdated = Time.time;
        graspedObject = null;
      }

      public void UpdateHand(Hand hand) {
        this.hand = hand;
        lastTimeUpdated = Time.time;
      }

      public void GraspObject(InteractionBehaviourBase obj) {
        graspedObject = obj;
        graspedObject.OnHandGrasp(hand);
      }

      public void ReleaseObject() {
        graspedObject.OnHandRelease(hand);
        graspedObject = null;
      }

      public void MarkUntracked() {
        isUntracked = true;
        graspedObject.OnHandLostTracking(hand);
      }

      public void MarkTimeout() {
        graspedObject.OnHandTimeout(hand);
        graspedObject = null;
        isUntracked = true;
        hand = null;
      }

      public void RegainTracking(Hand newHand) {
        int oldId = hand.Id;
        UpdateHand(newHand);

        isUntracked = false;
        graspedObject.OnHandRegainedTracking(newHand, oldId);
      }
    }
    #endregion
  }
}
