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

      if (enabled) {
        registerWithInteractionC(obj, shape);
      }
    }

    

    public void UnregisterInteractionObject(InteractionObject obj) {
      InteractionShape shape = _objects[obj];

      if (enabled) {
        unregisterWithInteractionC(obj, shape);
      }

      _objects.Remove(obj);
    }

    void OnEnable() {
      InteractionC.LeapIECreateScene(ref _scene);

      foreach (var pair in _objects) {
        registerWithInteractionC(pair.Key, pair.Value);
      }
    }

    void OnDisable() {
      foreach (var pair in _objects) {
        unregisterWithInteractionC(pair.Key, pair.Value);
      }

      InteractionC.LeapIEDestroyScene(ref _scene);
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

        InteractionC.LeapIEUpdateShape(ref _scene,
                                       ref instanceTransform,
                                       ref instanceHandle);
      }
    }

    private void updateIeTracking() {
      //TODO: Marshal hand array into InteractionC
    }

    private void simulateIe() {
      var _controllerTransform = new LEAP_IE_TRANSFORM();
      _controllerTransform.position = new LEAP_VECTOR(_leapProvider.transform.position);
      _controllerTransform.rotation = new LEAP_QUATERNION(_leapProvider.transform.rotation);
      _controllerTransform.wallTime = Time.fixedTime;

      InteractionC.LeapIEAdvance(ref _scene, ref _controllerTransform);
    }

    private void setObjectClassifications() {
      foreach (var pair in _objects) {
        var obj = pair.Key;
        var shape = pair.Value;

        var instanceHandle = shape.InstanceHandle;
        var classification = new LEAP_IE_SHAPE_CLASSIFICATION();

        InteractionC.LeapIEGetClassification(ref _scene,
                                             ref instanceHandle,
                                             ref classification);

        obj.SetClassification(classification.classification);
      }
    }

    private void registerWithInteractionC(InteractionObject obj, InteractionShape shape) {
      var shapeHandle = new LEAP_IE_SHAPE_DESCRIPTION_HANDLE();

      InteractionC.LeapIEAddShapeDescription(ref _scene,
                                             obj.ShapeDescription,
                                             ref shapeHandle);

      shape.ShapeHandle = shapeHandle;
      var shapeTransform = obj.IeTransform;

      var instanceHandle = new LEAP_IE_SHAPE_INSTANCE_HANDLE();

      InteractionC.LeapIECreateShape(ref _scene,
                                     ref shapeHandle,
                                     ref shapeTransform,
                                     ref instanceHandle);

      shape.InstanceHandle = instanceHandle;
    }

    private void unregisterWithInteractionC(InteractionObject obj, InteractionShape shape) {
      var shapeHandle = shape.ShapeHandle;
      InteractionC.LeapIERemoveShapeDescription(ref _scene,
                                                ref shapeHandle);

      var instanceHandle = shape.InstanceHandle;
      InteractionC.LeapIEDestroyShape(ref _scene,
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
