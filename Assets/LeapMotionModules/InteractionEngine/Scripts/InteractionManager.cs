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
    protected bool _enablePushing = true;

    [Header("Debug")]
    [Tooltip("Shows the debug output coming from the internal Interaction plugin.")]
    [SerializeField]
    protected bool _showDebugLines = true;

    [SerializeField]
    protected bool _showDebugOutput = true;
    #endregion

    #region INTERNAL FIELDS
    protected INTERACTION_SCENE _scene;
    private bool _hasSceneBeenCreated = false;
    private Coroutine _simulationCoroutine = null;

    protected ShapeDescriptionPool _shapeDescriptionPool;

    protected List<IInteractionBehaviour> _registeredBehaviours = new List<IInteractionBehaviour>();
    protected HashSet<IInteractionBehaviour> _misbehavingBehaviours = new HashSet<IInteractionBehaviour>();

    //Maps the Interaction instance handle to the behaviour
    //A mapping only exists if a shape instance has been created
    protected Dictionary<INTERACTION_SHAPE_INSTANCE_HANDLE, IInteractionBehaviour> _instanceHandleToBehaviour = new Dictionary<INTERACTION_SHAPE_INSTANCE_HANDLE, IInteractionBehaviour>();

    protected Dictionary<int, InteractionHand> _idToInteractionHand = new Dictionary<int, InteractionHand>();
    protected List<IInteractionBehaviour> _graspedBehaviours = new List<IInteractionBehaviour>();

    //A temp list that is recycled.  Used to remove items from _handIdToIeHand.
    private List<int> _handIdsToRemove = new List<int>();
    //A temp list that is recycled.  Used as the argument to OnHandsHold.
    private List<Hand> _holdingHands = new List<Hand>();
    //A temp list that is recycled.  Used to recieve results from InteractionC.
    private List<INTERACTION_SHAPE_INSTANCE_RESULTS> _resultList = new List<INTERACTION_SHAPE_INSTANCE_RESULTS>();
    //A temp list that is recycled.  Used to recieve debug logs from InteractionC.
    private List<string> _debugOutput = new List<string>();
    #endregion

    #region PUBLIC METHODS
    /// <summary>
    /// Gets the current debug flags for this manager.
    /// </summary>
    public virtual DebugFlags DebugFlags {
      get {
        DebugFlags flags = DebugFlags.None;
        if (_showDebugLines) {
          flags |= DebugFlags.Lines;
        }
        if (_showDebugOutput) {
          flags |= DebugFlags.Strings;
          flags |= DebugFlags.Logging;
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
    public IEnumerable<IInteractionBehaviour> RegisteredObjects {
      get {
        return _registeredBehaviours;
      }
    }

    /// <summary>
    /// Returns a collection of InteractionBehaviours that are currently being grasped by
    /// at least one hand.
    /// </summary>
    public IEnumerable<IInteractionBehaviour> GraspedObjects {
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
    public bool TryGetGraspedObject(int handId, out IInteractionBehaviour graspedObject) {
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
    public void RegisterInteractionBehaviour(IInteractionBehaviour interactionBehaviour) {
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
    public void UnregisterInteractionBehaviour(IInteractionBehaviour interactionBehaviour) {
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

    protected virtual void Awake() { }

    protected virtual void OnEnable() {
      Assert.IsFalse(_hasSceneBeenCreated, "Scene should not have been created yet");

      try {
        createScene();
      } catch (Exception e) {
        enabled = false;
        throw e;
      }

      applyDebugSettings();

      _shapeDescriptionPool = new ShapeDescriptionPool(_scene);

      Assert.AreEqual(_instanceHandleToBehaviour.Count, 0, "There should not be any instances before the creation step.");

      for (int i = 0; i < _registeredBehaviours.Count; i++) {
        IInteractionBehaviour interactionBehaviour = _registeredBehaviours[i];
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
        IInteractionBehaviour graspedBehaviour = interactionHand.graspedObject;
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
        destroyScene();
      }

      if (_simulationCoroutine != null) {
        StopCoroutine(_simulationCoroutine);
        _simulationCoroutine = null;
      }
    }

    protected virtual void LateUpdate() {
      dispatchOnHandsHolding(_leapProvider.CurrentFrame);

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

      dispatchOnHandsHolding(frame);

      updateInteractionRepresentations();

      updateTracking(frame);

      simulateInteraction();

      updateInteractionStateChanges(frame);

      // TODO: Pass a debug flag to disable calculating velocities.
      if (_enablePushing) {
        dispatchSimulationResults();
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
        IInteractionBehaviour interactionBehaviour = _registeredBehaviours[i];
        try {
          INTERACTION_SHAPE_INSTANCE_HANDLE shapeInstanceHandle = interactionBehaviour.ShapeInstanceHandle;

          INTERACTION_UPDATE_SHAPE_INFO updateInfo;
          INTERACTION_TRANSFORM updateTransform;
          interactionBehaviour.OnInteractionShapeUpdate(out updateInfo, out updateTransform);

          InteractionC.UpdateShapeInstance(ref _scene, ref updateTransform, ref updateInfo, ref shapeInstanceHandle);
        } catch (Exception e) {
          _misbehavingBehaviours.Add(interactionBehaviour);
          Debug.LogException(e);
        }
      }
    }

    protected virtual void dispatchOnHandsHolding(Frame frame) {
      var hands = frame.Hands;

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

    protected virtual void updateTracking(Frame frame) {
      int handCount = frame.Hands.Count;
      IntPtr ptr = HandArrayBuilder.CreateHandArray(frame);
      InteractionC.UpdateHands(ref _scene, (uint)handCount, ptr);
      StructAllocator.CleanupAllocations();
    }

    protected virtual void simulateInteraction() {
      var _controllerTransform = new INTERACTION_TRANSFORM();
      _controllerTransform.position = _leapProvider.transform.position.ToCVector();
      _controllerTransform.rotation = _leapProvider.transform.rotation.ToCQuaternion();
      _controllerTransform.wallTime = Time.fixedTime;

      InteractionC.UpdateController(ref _scene, ref _controllerTransform);
    }

    private bool toggle = false;
    protected virtual void updateInteractionStateChanges(Frame frame) {
      var hands = frame.Hands;

      if (Input.GetKeyDown(KeyCode.Space)) {
        toggle = !toggle;
      }

      //First loop through all the hands and get their classifications from the engine
      for (int i = 0; i < hands.Count; i++) {
        Hand hand = hands[i];

        INTERACTION_HAND_RESULT handResult;
        InteractionC.GetHandResult(ref _scene,
                                       (uint)hand.Id,
                                   out handResult);

        if (toggle) {
          handResult.classification = ManipulatorMode.Grasp;
          handResult.instanceHandle = _registeredBehaviours[0].ShapeInstanceHandle;
        }

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

            //Override the existing classification to force the hand to grab the old object
            handResult = new INTERACTION_HAND_RESULT();
            handResult.classification = ManipulatorMode.Grasp;
            handResult.handFlags = HandResultFlags.ManipulatorMode;
            handResult.instanceHandle = interactionHand.graspedObject.ShapeInstanceHandle;
            InteractionC.OverrideHandResult(ref _scene, (uint)hand.Id, ref handResult);

          } else {
            //Otherwise just create a new one
            interactionHand = new InteractionHand(hand);
          }

          //In both cases, associate the id with the new ieHand
          _idToInteractionHand[hand.Id] = interactionHand;
        }

        interactionHand.UpdateHand(hand);

        switch (handResult.classification) {
          case ManipulatorMode.Grasp:
            {
              IInteractionBehaviour interactionBehaviour = _instanceHandleToBehaviour[handResult.instanceHandle];
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
          case ManipulatorMode.Physics:
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
    }

    protected virtual void dispatchSimulationResults() {
      InteractionC.GetVelocities(ref _scene, _resultList);

      for (int i = 0; i < _resultList.Count; ++i) {
        INTERACTION_SHAPE_INSTANCE_RESULTS result = _resultList[i];
        IInteractionBehaviour interactionBehaviour = _instanceHandleToBehaviour[result.handle];

        try {
          interactionBehaviour.OnRecieveSimulationResults(result);
        } catch (Exception e) {
          _misbehavingBehaviours.Add(interactionBehaviour);
          Debug.LogException(e);
        }
      }
    }

    protected virtual void createInteractionShape(IInteractionBehaviour interactionBehaviour) {
      INTERACTION_SHAPE_DESCRIPTION_HANDLE descriptionHandle = interactionBehaviour.ShapeDescriptionHandle;
      INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle = new INTERACTION_SHAPE_INSTANCE_HANDLE();

      INTERACTION_CREATE_SHAPE_INFO createInfo;
      INTERACTION_TRANSFORM createTransform;
      interactionBehaviour.OnInteractionShapeCreationInfo(out createInfo, out createTransform);

      InteractionC.CreateShapeInstance(ref _scene, ref descriptionHandle, ref createTransform, ref createInfo, out instanceHandle);

      _instanceHandleToBehaviour[instanceHandle] = interactionBehaviour;

      interactionBehaviour.OnInteractionShapeCreated(instanceHandle);
    }

    protected virtual void destroyInteractionShape(IInteractionBehaviour interactionBehaviour) {
      INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle = interactionBehaviour.ShapeInstanceHandle;

      _instanceHandleToBehaviour.Remove(instanceHandle);

      InteractionC.DestroyShapeInstance(ref _scene, ref instanceHandle);

      interactionBehaviour.OnInteractionShapeDestroyed();
    }

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

    protected virtual void createScene() {
      INTERACTION_SCENE_INFO sceneInfo = getSceneInfo();
      string dataPath = Path.Combine(Application.streamingAssetsPath, _dataSubfolder);
      InteractionC.CreateScene(ref _scene, ref sceneInfo, dataPath);

      _hasSceneBeenCreated = true;
    }

    protected virtual void destroyScene() {
      InteractionC.DestroyScene(ref _scene);
      _hasSceneBeenCreated = false;
    }

    private INTERACTION_SCENE_INFO getSceneInfo() {
      INTERACTION_SCENE_INFO info = new INTERACTION_SCENE_INFO();
      info.gravity = Physics.gravity.ToCVector();
      info.sceneFlags = SceneInfoFlags.HasGravity;
      return info;
    }

    //A persistant structure for storing useful data about a hand as it interacts with objects
    protected class InteractionHand {
      public Hand hand { get; protected set; }
      public float lastTimeUpdated { get; protected set; }
      public IInteractionBehaviour graspedObject { get; protected set; }
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

      public void GraspObject(IInteractionBehaviour obj) {
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
