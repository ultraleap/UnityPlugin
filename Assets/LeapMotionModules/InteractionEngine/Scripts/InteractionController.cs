using UnityEngine;
using Leap;
using LeapInternal;
using InteractionEngine.Internal;

namespace InteractionEngine {

  public class InteractionController : MonoBehaviour {
    [SerializeField]
    private LeapProvider _leapProvider;

    private OneToOneMap<InteractionObject, InteractionShape> _objects = new OneToOneMap<InteractionObject, InteractionShape>();
    private LEAP_IE_SCENE _scene;

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

    void OnEnable() {
      InteractionC.CreateScene(ref _scene);

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
      Frame frame = _leapProvider.CurrentFrame;
      InteractionC.UpdateHands(ref _scene,
                               (uint)frame.Hands.Count,
                               frame.TempHandData);
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
        var classification = new LEAP_IE_SHAPE_CLASSIFICATION();

        InteractionC.GetClassification(ref _scene,
                                             ref instanceHandle,
                                             ref classification);

        obj.SetClassification(classification.classification);
      }
    }

    private void registerWithInteractionC(InteractionObject obj, InteractionShape shape) {
      var shapeHandle = new LEAP_IE_SHAPE_DESCRIPTION_HANDLE();

      InteractionC.AddShapeDescription(ref _scene,
                                             obj.ShapeDescription,
                                             ref shapeHandle);

      shape.ShapeHandle = shapeHandle;
      var shapeTransform = obj.IeTransform;

      var instanceHandle = new LEAP_IE_SHAPE_INSTANCE_HANDLE();

      InteractionC.CreateShape(ref _scene,
                                     ref shapeHandle,
                                     ref shapeTransform,
                                     ref instanceHandle);

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
