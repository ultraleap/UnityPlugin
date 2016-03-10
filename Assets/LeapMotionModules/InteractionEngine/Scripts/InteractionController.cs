using UnityEngine;
using System;
using Leap.Unity;
using LeapInternal;
using InteractionEngine.Internal;

namespace InteractionEngine {

  public class InteractionController : MonoBehaviour {
    [SerializeField]
    private LeapProvider _leapProvider;

    [SerializeField]
    private bool _showDebugLines = true;

    private OneToOneMap<InteractionObject, InteractionShape> _objects = new OneToOneMap<InteractionObject, InteractionShape>();
    private LEAP_IE_SCENE _scene;

    public eLeapIEDebugFlags DebugFlags {
      get {
        eLeapIEDebugFlags flags = eLeapIEDebugFlags.eLeapIEDebugFlags_None;
        if (_showDebugLines) {
          flags |= eLeapIEDebugFlags.eLeapIEDebugFlags_LinesInternal;
        }
        return flags;
      }
    }

    public LEAP_IE_SHAPE_DESCRIPTION_HANDLE RegisterShapeDescription(IntPtr shapePtr) {
      LEAP_IE_SHAPE_DESCRIPTION_HANDLE shapeHandle;
      InteractionC.AddShapeDescription(ref _scene, shapePtr, out shapeHandle);
      return shapeHandle;
    }

    public void UnregisterShapeDescription(ref LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle) {
      InteractionC.RemoveShapeDescription(ref _scene, ref handle);
    }

    public void Annotate(InteractionObject obj, uint type, uint bytes, IntPtr data) {
      if (!_objects.Contains(obj)) {
        throw new InvalidOperationException("Cannot annotate an object that has not been registered with the controller.");
      }

      var handle = _objects[obj].InstanceHandle;
      InteractionC.Annotate(ref _scene, ref handle, type, bytes, data);
    }

    public void Annotate<T>(InteractionObject obj, uint type, T t) where T : struct {
      try {
        IntPtr structDataPtr = StructAllocator.AllocateStruct<T>(t);
        Annotate(obj, type, (uint)StructMarshal<T>.Size, structDataPtr);
      } finally {
        StructAllocator.CleanupAllocations();
      }
    }

    public void RegisterInteractionObject(InteractionObject obj) {
      InteractionShape shape = new InteractionShape();
      _objects[obj] = shape;

      //Don't register right away if we are not enabled, registration will be done in OnEnable
      if (enabled) {
        registerWithInteractionC(obj, shape);
      }
    }

    public void UnregisterInteractionObject(InteractionObject obj) {
      InteractionShape shape = _objects[obj];

      //Don't unregister if we are not enabled, everything already got unregistered in OnDisable
      if (enabled) {
        unregisterWithInteractionC(obj, shape);
      }

      _objects.Remove(obj);
    }

    void OnValidate() {
      if (Application.isPlaying && isActiveAndEnabled) {
        applyDebugSettings();
      }
    }

    void OnEnable() {
      InteractionC.CreateScene(ref _scene);
      applyDebugSettings();

      foreach (var pair in _objects) {
        registerWithInteractionC(pair.Key, pair.Value);
      }
    }

    void OnDisable() {
      foreach (var pair in _objects) {
        unregisterWithInteractionC(pair.Key, pair.Value);
      }

      InteractionC.DestroyScene(ref _scene);
    }

    void FixedUpdate() {
      updateIeRepresentations();

      updateIeTracking();

      simulateIe();

      setObjectClassifications();

      if (_showDebugLines) {
        InteractionC.DrawDebugLines(ref _scene);
      }
    }

    private void applyDebugSettings() {
      InteractionC.EnableDebugFlags(ref _scene, (uint)DebugFlags);
    }

    private void updateIeRepresentations() {
      foreach (var pair in _objects) {
        var obj = pair.Key;
        var shape = pair.Value;

        var instanceTransform = obj.IeTransform;
        var instanceHandle = shape.InstanceHandle;

        InteractionC.UpdateShape(ref _scene,
                                       ref instanceTransform,
                                       ref instanceHandle);
      }
    }

    private void updateIeTracking() {
      InteractionC.UpdateHands(ref _scene,
                               _leapProvider.CurrentFrame);
    }

    private void simulateIe() {
      var _controllerTransform = new LEAP_IE_TRANSFORM();
      _controllerTransform.position = new LEAP_VECTOR(_leapProvider.transform.position);
      _controllerTransform.rotation = new LEAP_QUATERNION(_leapProvider.transform.rotation);
      _controllerTransform.wallTime = Time.fixedTime;

      InteractionC.UpdateController(ref _scene, ref _controllerTransform);
    }

    private void setObjectClassifications() {
      foreach (var pair in _objects) {
        var obj = pair.Key;
        var shape = pair.Value;

        var instanceHandle = shape.InstanceHandle;
        LEAP_IE_SHAPE_CLASSIFICATION classification;

        InteractionC.GetClassification(ref _scene,
                                       ref instanceHandle,
                                       out classification);

        obj.SetClassification(classification.classification);
      }
    }

    private void registerWithInteractionC(InteractionObject obj, InteractionShape shape) {
      var shapeHandle = obj.ShapeHandle;
      var shapeTransform = obj.IeTransform;

      LEAP_IE_SHAPE_INSTANCE_HANDLE instanceHandle;

      InteractionC.CreateShape(ref _scene,
                               ref shapeHandle,
                               ref shapeTransform,
                               out instanceHandle);

      shape.ShapeHandle = shapeHandle;
      shape.InstanceHandle = instanceHandle;
    }

    private void unregisterWithInteractionC(InteractionObject obj, InteractionShape shape) {
      var shapeHandle = shape.ShapeHandle;
      InteractionC.RemoveShapeDescription(ref _scene,
                                          ref shapeHandle);

      var instanceHandle = shape.InstanceHandle;
      InteractionC.DestroyShape(ref _scene,
                                ref instanceHandle);

      shape.InstanceHandle = new LEAP_IE_SHAPE_INSTANCE_HANDLE();
      shape.ShapeHandle = new LEAP_IE_SHAPE_DESCRIPTION_HANDLE();
    }

    private class InteractionShape {
      public LEAP_IE_SHAPE_DESCRIPTION_HANDLE ShapeHandle;
      public LEAP_IE_SHAPE_INSTANCE_HANDLE InstanceHandle;
    }
  }
}
