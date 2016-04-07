using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  public class InteractionManager : MonoBehaviour {
    #region SERIALIZED FIELDS
    [SerializeField]
    protected LeapProvider _leapProvider;

    [SerializeField]
    protected string _dataSubfolder = "InteractionEngine";

    [Tooltip("The amount of time a Hand can remain untracked while also still grasping an object.")]
    [SerializeField]
    protected float _untrackedTimeout = 0.5f;

    [Tooltip("Allow the Interaction plugin to modify object velocities when pushing.")]
    [SerializeField]
    protected bool _modifyVelocities = true;

    [Header("Debug")]
    [Tooltip("Shows the debug output coming from the internal Interaction plugin.")]
    [SerializeField]
    protected bool _showDebugLines = true;

    [SerializeField]
    protected bool _showDebugOutput = true;
    #endregion

    #region INTERNAL FIELDS
    protected List<InteractionBehaviourBase> _registeredBehaviours;
    protected HashSet<InteractionBehaviourBase> _misbehavingBehaviours;

    //Maps the Interaction instance handle to the behaviour
    //A mapping only exists if a shape instance has been created
    protected Dictionary<LEAP_IE_SHAPE_INSTANCE_HANDLE, InteractionBehaviourBase> _instanceHandleToBehaviour;

    protected Dictionary<int, InteractionHand> _idToInteractionHand;
    protected List<InteractionBehaviourBase> _graspedBehaviours;

    protected ShapeDescriptionPool _shapeDescriptionPool;

    private bool _hasSceneBeenCreated = false;
    private Coroutine _simulationCoroutine = null;
    protected LEAP_IE_SCENE _scene;

    //A temp list that is recycled.  Used to remove items from _handIdToIeHand.
    private List<int> _handIdsToRemove;
    //A temp list that is recycled.  Used as the argument to OnHandsHold.
    private List<Hand> _holdingHands;

    private List<LEAP_IE_SHAPE_INSTANCE_RESULTS> _resultList;

    private List<string> _debugOutput;
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
        if (_showDebugOutput) {
          flags |= eLeapIEDebugFlags.eLeapIEListenerFlags_Strings;
          flags |= eLeapIEDebugFlags.eLeapIEDebugFlags_Logging;
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
    /// Returns a collection of InteractionBehaviours that are currently registered with this manager.
    /// </summary>
    public IEnumerable<InteractionBehaviourBase> RegisteredObjects {
      get {
        return _registeredBehaviours;
      }
    }

    /// <summary>
    /// Returns a collection of InteractionBehaviours that are currently being grasped by
    /// at least one hand.
    /// </summary>
    public IEnumerable<InteractionBehaviourBase> GraspedObjects {
      get {
        return _graspedBehaviours;
      }
    }

    public void UpdateSceneInfo() {
      var info = getSceneInfo();
      InteractionC.UpdateSceneInfo(ref _scene, ref info);
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
      if (_registeredBehaviours.Contains(interactionBehaviour)) {
        throw new InvalidOperationException("Interaction Behaviour " + interactionBehaviour + " cannot be registered because " +
                                            "it is already registered with this manager.");
      }

      _registeredBehaviours.Add(interactionBehaviour);

      try {
        interactionBehaviour.OnRegister();
      } catch (Exception e) {
        _misbehavingBehaviours.Add(interactionBehaviour);
        throw e;
      }

      //Don't create right away if we are not enabled, creation will be done in OnEnable
      if (_hasSceneBeenCreated) {
        createInteractionShape(interactionBehaviour);
      }
    }

    /// <summary>
    /// Unregisters an InteractionObject from this manager.  This removes it from the internal
    /// scene and prevents any further interaction.
    /// </summary>
    /// <param name="interactionBehaviour"></param>
    public void UnregisterInteractionBehaviour(InteractionBehaviourBase interactionBehaviour) {
      if (!_registeredBehaviours.Contains(interactionBehaviour)) {
        throw new InvalidOperationException("Interaction Behaviour " + interactionBehaviour + " cannot be unregistered because " +
                                            "it is not currently registered with this manager.");
      }

      _registeredBehaviours.Remove(interactionBehaviour);

      if (_graspedBehaviours.Remove(interactionBehaviour)) {
        foreach (var interactionHand in _idToInteractionHand.Values) {
          if (interactionHand.graspedObject == interactionBehaviour) {
            try {
              interactionHand.ReleaseObject();
            } catch (Exception e) {
              //Only log to console
              //We want to continue so we can destroy the shape and dispatch OnUnregister
              Debug.LogException(e);
            }
            break;
          }
        }
      }

      //Don't destroy if we are not enabled, everything already got destroyed in OnDisable
      if (_hasSceneBeenCreated) {
        try {
          destroyInteractionShape(interactionBehaviour);
        } catch (Exception e) {
          //Like above, only log to console so we can dispatch OnUnregister
          Debug.LogException(e);
        }
      }

      interactionBehaviour.OnUnregister();
    }
    #endregion

    #region UNITY CALLBACKS
    protected virtual void Reset() {
      if (_leapProvider == null) {
        _leapProvider = FindObjectOfType<LeapProvider>();
      }
    }

    protected virtual void OnValidate() {
      if (Application.isPlaying && _hasSceneBeenCreated) {
        //Allow the debug lines to be toggled while the scene is playing
        applyDebugSettings();
      }

      //Timeout must be positive
      if (_untrackedTimeout < 0) {
        _untrackedTimeout = 0;
      }
    }

    protected virtual void Awake() {
      _registeredBehaviours = new List<InteractionBehaviourBase>();
      _misbehavingBehaviours = new HashSet<InteractionBehaviourBase>();
      _instanceHandleToBehaviour = new Dictionary<LEAP_IE_SHAPE_INSTANCE_HANDLE, InteractionBehaviourBase>();
      _graspedBehaviours = new List<InteractionBehaviourBase>();
      _idToInteractionHand = new Dictionary<int, InteractionHand>();
      _handIdsToRemove = new List<int>();
      _holdingHands = new List<Hand>();
      _resultList = new List<LEAP_IE_SHAPE_INSTANCE_RESULTS>();
      _debugOutput = new List<string>();
    }

    protected virtual void OnEnable() {
      Assert.IsFalse(_hasSceneBeenCreated, "Scene should not have been created yet");

      try {
        LEAP_IE_SCENE_INFO sceneInfo = getSceneInfo();
        string dataPath = Path.Combine(Application.streamingAssetsPath, _dataSubfolder);
        InteractionC.CreateScene(ref _scene, ref sceneInfo, dataPath);

        _hasSceneBeenCreated = true;
        applyDebugSettings();
      } catch (Exception e) {
        enabled = false;
        throw e;
      }

      _shapeDescriptionPool = new ShapeDescriptionPool(_scene);

      Assert.AreEqual(_instanceHandleToBehaviour.Count, 0, "There should not be any instances before the creation step.");

      for (int i = 0; i < _registeredBehaviours.Count; i++) {
        InteractionBehaviourBase interactionBehaviour = _registeredBehaviours[i];
        try {
          createInteractionShape(interactionBehaviour);
        } catch (Exception e) {
          _misbehavingBehaviours.Add(interactionBehaviour);
          Debug.LogException(e);
        }
      }

      _simulationCoroutine = StartCoroutine(simulationLoop());
    }

    protected virtual void OnDisable() {
      foreach (var interactionHand in _idToInteractionHand.Values) {
        InteractionBehaviourBase graspedBehaviour = interactionHand.graspedObject;
        if (graspedBehaviour != null) {
          try {
            interactionHand.ReleaseObject();
          } catch (Exception e) {
            _misbehavingBehaviours.Add(graspedBehaviour);
            Debug.LogException(e);
          }
        }
      }

      unregisterMisbehavingBehaviours();

      _idToInteractionHand.Clear();
      _graspedBehaviours.Clear();

      for (int i = 0; i < _registeredBehaviours.Count; i++) {
        try {
          destroyInteractionShape(_registeredBehaviours[i]);
        } catch (Exception e) {
          Debug.LogException(e);
        }
      }

      Assert.AreEqual(_instanceHandleToBehaviour.Count, 0, "All instances should have been destroyed.");

      if (_shapeDescriptionPool != null) {
        _shapeDescriptionPool.RemoveAllShapes();
        _shapeDescriptionPool = null;
      }

      if (_hasSceneBeenCreated) {
        InteractionC.DestroyScene(ref _scene);
        _hasSceneBeenCreated = false;
      }

      if (_simulationCoroutine != null) {
        StopCoroutine(_simulationCoroutine);
        _simulationCoroutine = null;
      }
    }

    protected virtual void LateUpdate() {
      unregisterMisbehavingBehaviours();
    }

    protected virtual void OnGUI() {
      if (_showDebugOutput) {
        for (int i = 0; i < _debugOutput.Count; i++) {
          GUILayout.Label(_debugOutput[i]);
        }
      }
    }
    #endregion

    #region INTERNAL METHODS
    private IEnumerator simulationLoop() {
      WaitForFixedUpdate fixedUpdate = new WaitForFixedUpdate();
      while (true) {
        yield return fixedUpdate;

        try {
          simulateFrame(_leapProvider.CurrentFrame);

          if (_showDebugLines) {
            InteractionC.DrawDebugLines(ref _scene);
          }

          if (_showDebugOutput) {
            InteractionC.GetDebugStrings(ref _scene, _debugOutput);
          }
        } catch (Exception e) {
          //Catch the error so that the loop doesn't terminate
          Debug.LogException(e);
        }
      }
    }

    protected virtual void simulateFrame(Frame frame) {
      for (int i = 0; i < _registeredBehaviours.Count; i++) {
        _registeredBehaviours[i].OnPreSolve();
      }

      updateInteractionRepresentations();

      updateTracking(frame);

      simulateInteraction();

      updateInteractionStateChanges();

      // TODO: Pass a debug flag to disable calculating velocities.
      if (_modifyVelocities) {
        setObjectVelocities();
      }

      for (int i = 0; i < _registeredBehaviours.Count; i++) {
        _registeredBehaviours[i].OnPostSolve();
      }
    }

    protected virtual void applyDebugSettings() {
      InteractionC.EnableDebugFlags(ref _scene, (uint)DebugFlags);
    }

    protected virtual void updateInteractionRepresentations() {
      for (int i = 0; i < _registeredBehaviours.Count; i++) {
        InteractionBehaviourBase interactionBehaviour = _registeredBehaviours[i];
        try {
          LEAP_IE_SHAPE_INSTANCE_HANDLE shapeInstanceHandle = interactionBehaviour.ShapeInstanceHandle;
          LEAP_IE_TRANSFORM interactionTransform = interactionBehaviour.InteractionTransform;
          LEAP_IE_UPDATE_SHAPE_INFO updateInfo = interactionBehaviour.OnInteractionShapeUpdate();
          InteractionC.UpdateShape(ref _scene, ref interactionTransform, ref updateInfo, ref shapeInstanceHandle);
        } catch (Exception e) {
          _misbehavingBehaviours.Add(interactionBehaviour);
          Debug.LogException(e);
        }
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
      _controllerTransform.position = _leapProvider.transform.position.ToCVector();
      _controllerTransform.rotation = _leapProvider.transform.rotation.ToCQuaternion();
      _controllerTransform.wallTime = Time.fixedTime;

      InteractionC.UpdateController(ref _scene, ref _controllerTransform);
    }

    protected virtual void updateInteractionStateChanges() {
      var hands = _leapProvider.CurrentFrame.Hands;

      //First loop through all the hands and get their classifications from the engine
      for (int i = 0; i < hands.Count; i++) {
        Hand hand = hands[i];

        LEAP_IE_HAND_RESULT handResult;
        LEAP_IE_SHAPE_INSTANCE_HANDLE instance;
        InteractionC.GetHandResult(ref _scene,
                                       (uint)hand.Id,
                                   out handResult,
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

            try {
              //This also dispatched InteractionObject.OnHandRegainedTracking()
              interactionHand.RegainTracking(hand);
            } catch (Exception e) {
              _misbehavingBehaviours.Add(interactionHand.graspedObject);
              Debug.LogException(e);
              continue;
            }

          } else {
            //Otherwise just create a new one
            interactionHand = new InteractionHand(hand);
          }

          //In both cases, associate the id with the new ieHand
          _idToInteractionHand[hand.Id] = interactionHand;
        }

        interactionHand.UpdateHand(hand);

        switch (handResult.classification) {
          case eLeapIEClassification.eLeapIEClassification_Grasp:
            {
              InteractionBehaviourBase interactionBehaviour = _instanceHandleToBehaviour[instance];
              if (interactionHand.graspedObject == null) {
                _graspedBehaviours.Add(interactionBehaviour);

                try {
                  interactionHand.GraspObject(interactionBehaviour);
                } catch (Exception e) {
                  _misbehavingBehaviours.Add(interactionBehaviour);
                  Debug.LogException(e);
                  continue;
                }

              }
              break;
            }
          case eLeapIEClassification.eLeapIEClassification_Physics:
            {
              if (interactionHand.graspedObject != null) {
                _graspedBehaviours.Remove(interactionHand.graspedObject);

                try {
                  interactionHand.ReleaseObject();
                } catch (Exception e) {
                  _misbehavingBehaviours.Add(interactionHand.graspedObject);
                  Debug.LogException(e);
                  continue;
                }

              }
              break;
            }
          default:
            throw new InvalidOperationException("Unexpected classification " + handResult.classification);
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
            try {
              //This also dispatches InteractionObject.OnHandLostTracking()
              ieHand.MarkUntracked();
            } catch (Exception e) {
              _misbehavingBehaviours.Add(ieHand.graspedObject);
              Debug.LogException(e);
            }
          }

          //If the age is longer than the timeout, we also remove it from the list
          if (handAge > _untrackedTimeout) {
            _handIdsToRemove.Add(id);

            try {
              //This also dispatched InteractionObject.OnHandTimeout()
              ieHand.MarkTimeout();
            } catch (Exception e) {
              _misbehavingBehaviours.Add(ieHand.graspedObject);
              Debug.LogException(e);
            }

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

        try {
          interactionBehaviour.OnHandsHold(_holdingHands);
        } catch (Exception e) {
          _misbehavingBehaviours.Add(interactionBehaviour);
          Debug.LogException(e);
        }

        _holdingHands.Clear();
      }
    }

    protected virtual void setObjectVelocities() {
      InteractionC.GetVelocities(ref _scene, _resultList);

      for (int i = 0; i < _resultList.Count; ++i) {
        LEAP_IE_SHAPE_INSTANCE_RESULTS result = _resultList[i];
        InteractionBehaviourBase interactionBehaviour = _instanceHandleToBehaviour[result.handle];

        try {
          interactionBehaviour.OnRecieveSimulationResults(result);
        } catch (Exception e) {
          _misbehavingBehaviours.Add(interactionBehaviour);
          Debug.LogException(e);
        }
      }
    }

    protected virtual void createInteractionShape(InteractionBehaviourBase interactionBehaviour) {
      LEAP_IE_SHAPE_DESCRIPTION_HANDLE descriptionHandle = interactionBehaviour.ShapeDescriptionHandle;
      LEAP_IE_SHAPE_INSTANCE_HANDLE instanceHandle = new LEAP_IE_SHAPE_INSTANCE_HANDLE();
      LEAP_IE_TRANSFORM interactionTransform = interactionBehaviour.InteractionTransform;
      LEAP_IE_CREATE_SHAPE_INFO createInfo = new LEAP_IE_CREATE_SHAPE_INFO();
      createInfo.shapeFlags = eLeapIEShapeFlags.eLeapIEShapeFlags_HasRigidBody | eLeapIEShapeFlags.eLeapIEShapeFlags_GravityEnabled;
      InteractionC.CreateShape(ref _scene, ref descriptionHandle, ref interactionTransform, ref createInfo, out instanceHandle);

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

    #region INTERNAL

    private void unregisterMisbehavingBehaviours() {
      if (_misbehavingBehaviours.Count > 0) {
        foreach (var interactionBehaviour in _misbehavingBehaviours) {
          if (interactionBehaviour != null) {
            try {
              UnregisterInteractionBehaviour(interactionBehaviour);
            } catch (Exception e) {
              Debug.LogException(e);
            }
          }
        }
        _misbehavingBehaviours.Clear();
      }
    }

    private LEAP_IE_SCENE_INFO getSceneInfo() {
      LEAP_IE_SCENE_INFO info = new LEAP_IE_SCENE_INFO();
      info.gravity = Physics.gravity.ToCVector();
      info.sceneFlags = eLeapIESceneFlags.eLeapIESceneFlags_HasGravity;
      return info;
    }

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

        //TODO: Need to force engine to update the grabbed state!

        isUntracked = false;
        graspedObject.OnHandRegainedTracking(newHand, oldId);
      }
    }
    #endregion
  }
}
