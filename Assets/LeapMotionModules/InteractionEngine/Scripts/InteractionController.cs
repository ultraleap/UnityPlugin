using UnityEngine;
using System;
using System.Collections.Generic;
using Leap;
using Leap.Unity;
using LeapInternal;
using InteractionEngine.CApi;

namespace InteractionEngine {

  public class InteractionController : MonoBehaviour {

    #region SERIALIZED FIELDS
    [SerializeField]
    protected LeapProvider _leapProvider;

    [Tooltip("If disabled the simulation will not progress, but will still maintain it's state.")]
    [SerializeField]
    protected bool _enableSimulation = true;

    [Tooltip("Shows the debug output coming from the internal Interaction plugin.")]
    [SerializeField]
    protected bool _showDebugLines = true;
    #endregion

    #region INTERNAL FIELDS
    protected Dictionary<LEAP_IE_SHAPE_INSTANCE_HANDLE, RegisteredObject> _instanceToRegistry;
    protected Dictionary<InteractionObject, RegisteredObject> _objToRegistry;
    protected List<InteractionObject> _graspedObjects;

    protected ShapeDescriptionPool _shapeDescriptionPool;

    protected LEAP_IE_SCENE _scene;
    #endregion

    #region PUBLIC METHODS
    /// <summary>
    /// Gets the current debug flags for this Controller.
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
    /// for this controller.  Using the pool can be more efficient since identical shapes
    /// can be automatically combined to save memory.  Shape descriptions aquired from this
    /// pool will be destroyed when this controller is disabled.
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
        return _graspedObjects.Count != 0;
      }
    }

    /// <summary>
    /// Returns a collection of InteractionObjects that are currently being grasped by
    /// at least one hand.
    /// </summary>
    public IEnumerable<InteractionObject> GraspedObjects {
      get {
        return _graspedObjects;
      }
    }

    /// <summary>
    /// Tries to find an InteractionObject that is currently being grasped by a Hand with
    /// the given ID.
    /// </summary>
    /// <param name="handId"></param>
    /// <param name="graspedObject"></param>
    /// <returns></returns>
    public bool TryGetGraspedObject(int handId, out InteractionObject graspedObject) {
      for (int i = 0; i < _graspedObjects.Count; i++) {
        var iObj = _graspedObjects[i];
        if (iObj.IsBeingGraspedByHand(handId)) {
          graspedObject = iObj;
          return true;
        }
      }

      graspedObject = null;
      return false;
    }

    /// <summary>
    /// Registers an InteractionObject with this Controller, which automatically adds the objects
    /// representation into the internal interaction scene.  If the controller is disabled, 
    /// the registration will still succeed and the object will be added to the internal scene
    /// when the controller is next enabled.
    /// </summary>
    /// <param name="obj"></param>
    public void RegisterInteractionObject(InteractionObject obj) {
      RegisteredObject registeredObj = new RegisteredObject();
      registeredObj.interactionObject = obj;

      _objToRegistry[obj] = registeredObj;

      //Don't create right away if we are not enabled, creation will be done in OnEnable
      if (enabled) {
        createIEShape(registeredObj);
      }
    }

    /// <summary>
    /// Unregisters an InteractionObject from this Controller.  This removes it from the internal
    /// scene and prevents any further interaction.
    /// </summary>
    /// <param name="obj"></param>
    public void UnregisterInteractionObject(InteractionObject obj) {
      RegisteredObject registeredObj = _objToRegistry[obj];

      //Don't destroy if we are not enabled, everything already got destroyed in OnDisable
      if (enabled) {
        destroyIEShape(registeredObj);
      }

      _objToRegistry.Remove(obj);
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
      _instanceToRegistry = new Dictionary<LEAP_IE_SHAPE_INSTANCE_HANDLE, RegisteredObject>();
      _objToRegistry = new Dictionary<InteractionObject, RegisteredObject>();
      _graspedObjects = new List<InteractionObject>();
    }

    protected virtual void OnEnable() {
      InteractionC.CreateScene(ref _scene);
      _shapeDescriptionPool = new ShapeDescriptionPool(_scene);
      applyDebugSettings();

      foreach (var registeredObj in _objToRegistry.Values) {
        createIEShape(registeredObj);
      }
    }

    protected virtual void OnDisable() {
      foreach (var registeredObj in _instanceToRegistry.Values) {
        destroyIEShape(registeredObj);
      }

      _shapeDescriptionPool.RemoveAllShapes();
      _shapeDescriptionPool = null;
      InteractionC.DestroyScene(ref _scene);
    }

    protected virtual void FixedUpdate() {
      if (_enableSimulation) {
        simulateFrame(_leapProvider.CurrentFrame);
      }

      if (_showDebugLines) {
        InteractionC.DrawDebugLines(ref _scene);
      }
    }
    #endregion

    #region INTERNAL METHODS
    protected virtual void simulateFrame(Frame frame) {
      updateIeRepresentations();

      updateIeTracking(frame);

      simulateIe();

      setObjectClassifications();
    }

    protected virtual void applyDebugSettings() {
      InteractionC.EnableDebugFlags(ref _scene, (uint)DebugFlags);
    }

    protected virtual void updateIeRepresentations() {
      foreach (var registeredObj in _instanceToRegistry.Values) {
        registeredObj.UpdateIERepresentation(ref _scene);
      }
    }

    protected virtual void updateIeTracking(Frame frame) {
      int handCount = frame.Hands.Count;
      IntPtr ptr = HandArrayBuilder.CreateHandArray(frame);
      InteractionC.UpdateHands(ref _scene, (uint)handCount, ptr);
      StructAllocator.CleanupAllocations();
    }

    protected virtual void simulateIe() {
      var _controllerTransform = new LEAP_IE_TRANSFORM();
      _controllerTransform.position = new LEAP_VECTOR(_leapProvider.transform.position);
      _controllerTransform.rotation = new LEAP_QUATERNION(_leapProvider.transform.rotation);
      _controllerTransform.wallTime = Time.fixedTime;

      InteractionC.UpdateController(ref _scene, ref _controllerTransform);
    }

    protected virtual void setObjectClassifications() {
      Frame currFrame = _leapProvider.CurrentFrame;
      for (int i = 0; i < currFrame.Hands.Count; i++) {
        Hand hand = currFrame.Hands[i];

        LEAP_IE_HAND_CLASSIFICATION classification;
        LEAP_IE_SHAPE_INSTANCE_HANDLE instance;
        InteractionC.GetClassification(ref _scene,
                                       (uint)hand.Id,
                                       out classification,
                                       out instance);

        RegisteredObject registeredObj = _instanceToRegistry[instance];

        switch (classification.classification) {
          case eLeapIEClassification.eLeapIEClassification_Grasp:
            {
              registeredObj.AddHoldingHand(hand);
              if (!_graspedObjects.Contains(registeredObj.interactionObject)) {
                _graspedObjects.Add(registeredObj.interactionObject);
                registeredObj.interactionObject.OnHandGrasp(hand);
              }
              break;
            }
          case eLeapIEClassification.eLeapIEClassification_Physics:
            {
              if (_graspedObjects.Contains(registeredObj.interactionObject)) {
                _graspedObjects.Remove(registeredObj.interactionObject);
                registeredObj.interactionObject.OnHandRelease(hand);
              }
              break;
            }
          default:
            throw new InvalidOperationException("Unexpected classification " + classification.classification);
        }
      }

      for (int i = 0; i < _graspedObjects.Count; i++) {
        var iObj = _graspedObjects[i];
        var registeredObj = _objToRegistry[iObj];
        registeredObj.DispatchHoldingCallback();
      }
    }

    protected virtual void createIEShape(RegisteredObject registeredObj) {
      registeredObj.CreateIEShape(ref _scene);
      _instanceToRegistry[registeredObj.instanceHandle] = registeredObj;
    }

    protected virtual void destroyIEShape(RegisteredObject registeredObj) {
      _instanceToRegistry.Remove(registeredObj.instanceHandle);
      registeredObj.DestroyIEShape(ref _scene);
    }
    #endregion

    #region INTERNAL CLASSES
    protected class RegisteredObject {
      public InteractionObject interactionObject;
      public LEAP_IE_SHAPE_DESCRIPTION_HANDLE shapeHandle;
      public LEAP_IE_SHAPE_INSTANCE_HANDLE instanceHandle;

      private List<Hand> _holdingHandList = new List<Hand>();

      public void UpdateIERepresentation(ref LEAP_IE_SCENE scene) {
        LEAP_IE_TRANSFORM t = interactionObject.GetIETransform();
        InteractionC.UpdateShape(ref scene, ref t, ref instanceHandle);
      }

      public void CreateIEShape(ref LEAP_IE_SCENE scene) {
        shapeHandle = interactionObject.GetShapeDescription();
        LEAP_IE_TRANSFORM t = interactionObject.GetIETransform();
        InteractionC.CreateShape(ref scene, ref shapeHandle, ref t, out instanceHandle);
      }

      public void DestroyIEShape(ref LEAP_IE_SCENE scene) {
        InteractionC.DestroyShape(ref scene, ref instanceHandle);
      }

      public void AddHoldingHand(Hand hand) {
        _holdingHandList.Add(hand);
      }

      public void DispatchHoldingCallback() {
        interactionObject.OnHandsHold(_holdingHandList);
        _holdingHandList.Clear();
      }
    }
    #endregion
  }
}
