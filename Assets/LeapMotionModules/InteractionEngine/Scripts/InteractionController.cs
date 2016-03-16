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

    [Tooltip("If disabled, objects will still be able to be registers and unregistered, but the simulation will not progress.")]
    [SerializeField]
    protected bool _enableSimulation = true;

    [SerializeField]
    protected bool _showDebugLines = true;
    #endregion

    #region INTERNAL FIELDS
    protected Dictionary<LEAP_IE_SHAPE_INSTANCE_HANDLE, RegisteredObject> _instanceToRegistry;
    protected Dictionary<InteractionObject, RegisteredObject> _objToRegistry;
    protected ShapeDescriptionPool _shapeDescriptionPool;

    protected List<InteractionObject> _graspedObjects;
    protected LEAP_IE_SCENE _scene;
    #endregion

    #region PUBLIC METHODS
    public eLeapIEDebugFlags DebugFlags {
      get {
        eLeapIEDebugFlags flags = eLeapIEDebugFlags.eLeapIEDebugFlags_None;
        if (_showDebugLines) {
          flags |= eLeapIEDebugFlags.eLeapIEDebugFlags_Lines;
        }
        return flags;
      }
    }

    public ShapeDescriptionPool ShapePool {
      get {
        return _shapeDescriptionPool;
      }
    }

    public void RegisterInteractionObject(InteractionObject obj) {
      RegisteredObject registeredObj = new RegisteredObject();
      registeredObj.InteractionObject = obj;

      _objToRegistry[obj] = registeredObj;

      //Don't create right away if we are not enabled, creation will be done in OnEnable
      if (enabled) {
        createIEShape(registeredObj);
      }
    }

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
    protected virtual void OnValidate() {
      if (Application.isPlaying && isActiveAndEnabled) {
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

        //Ungrasp objects that were grasped before
        for (int j = _graspedObjects.Count - 1; j >= 0; j--) {
          if (_graspedObjects[j].IsBeingGraspedByHand(hand.Id)) {
            if (classification.classification == eLeapIEClassification.eLeapIEClassification_Physics) {
              _graspedObjects[j].EndHandGrasp(hand.Id);
              _graspedObjects.RemoveAt(j);
            }
          }
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
        LEAP_IE_TRANSFORM t = InteractionObject.IeTransform;
        InteractionC.UpdateShape(ref scene, ref t, ref InstanceHandle);
      }

      public void CreateIEShape(ref LEAP_IE_SCENE scene) {
        ShapeHandle = InteractionObject.GetShapeDescription();
        LEAP_IE_TRANSFORM t = InteractionObject.IeTransform;
        InteractionC.CreateShape(ref scene, ref ShapeHandle, ref t, out InstanceHandle);
      }

      public void DestroyIEShape(ref LEAP_IE_SCENE scene) {
        InteractionC.DestroyShape(ref scene, ref InstanceHandle);
      }
    }
    #endregion
  }
}
