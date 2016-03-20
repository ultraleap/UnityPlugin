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
    /// Registers an InteractionObject with this Controller, which automatically adds the objects
    /// representation into the internal interaction scene.  If the controller is disabled, 
    /// the registration will still succeed and the object will be added to the internal scene
    /// when the controller is next enabled.
    /// </summary>
    /// <param name="obj"></param>
    public void RegisterInteractionObject(InteractionObject obj) {
      RegisteredObject registeredObj = new RegisteredObject();
      registeredObj.InteractionObject = obj;

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

        switch (classification.classification) {
          case eLeapIEClassification.eLeapIEClassification_Grasp:
            {
              var iObj = _instanceToRegistry[instance].InteractionObject;
              iObj.OnGraspEnter(hand.Id);
              break;
            }
          case eLeapIEClassification.eLeapIEClassification_Physics:
            {
              var iObj = _instanceToRegistry[instance].InteractionObject;
              iObj.OnGraspExit(hand.Id);
              break;
            }
          default:
            throw new InvalidOperationException("Unexpected classification " + classification.classification);
        }
      }
    }

    protected virtual void createIEShape(RegisteredObject registeredObj) {
      registeredObj.CreateIEShape(ref _scene);
      _instanceToRegistry[registeredObj.InstanceHandle] = registeredObj;
    }

    protected virtual void destroyIEShape(RegisteredObject registeredObj) {
      _instanceToRegistry.Remove(registeredObj.InstanceHandle);
      registeredObj.DestroyIEShape(ref _scene);
    }
    #endregion

    #region INTERNAL CLASSES
    protected class RegisteredObject {
      public InteractionObject InteractionObject;
      public LEAP_IE_SHAPE_DESCRIPTION_HANDLE ShapeHandle;
      public LEAP_IE_SHAPE_INSTANCE_HANDLE InstanceHandle;

      public void UpdateIERepresentation(ref LEAP_IE_SCENE scene) {
        LEAP_IE_TRANSFORM t = InteractionObject.GetIETransform();
        InteractionC.UpdateShape(ref scene, ref t, ref InstanceHandle);
      }

      public void CreateIEShape(ref LEAP_IE_SCENE scene) {
        ShapeHandle = InteractionObject.GetShapeDescription();
        LEAP_IE_TRANSFORM t = InteractionObject.GetIETransform();
        InteractionC.CreateShape(ref scene, ref ShapeHandle, ref t, out InstanceHandle);
      }

      public void DestroyIEShape(ref LEAP_IE_SCENE scene) {
        InteractionC.DestroyShape(ref scene, ref InstanceHandle);
      }
    }
    #endregion
  }
}
