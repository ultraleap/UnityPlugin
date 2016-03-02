using UnityEngine;
using System;
using System.Collections.Generic;
using Leap;
using LeapInternal;
using InteractionEngine.Internal;

namespace InteractionEngine {

  public class InteractionController : MonoBehaviour {
    [SerializeField]
    private LeapProvider _leapProvider;

    private HashSet<InteractionObject> _objects = new HashSet<InteractionObject>();
    private LEAP_IE_SCENE _scene;

    public LEAP_IE_SCENE Scene {
      get {
        return _scene;
      }
    }

    public LEAP_IE_SHAPE_DESCRIPTION_HANDLE RegisterShapeDescription(IntPtr descPtr) {
      var handle = new LEAP_IE_SHAPE_DESCRIPTION_HANDLE();
      InteractionC.LeapIEAddShapeDescription(ref _scene, descPtr, ref handle);
      return handle;
    }

    public void UnregisterShapeDescription(ref LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle) {
      InteractionC.LeapIERemoveShapeDescription(ref _scene, ref handle);
    }

    public LEAP_IE_SHAPE_INSTANCE_HANDLE RegisterInteractionObject(InteractionObject interactionObject) {
      _objects.Add(interactionObject);

      var shapeHandle = interactionObject.ShapeDescriptionHandle;
      var shapeTransform = interactionObject.IeTransform;
      var instanceHandle = new LEAP_IE_SHAPE_INSTANCE_HANDLE();

      InteractionC.LeapIECreateShape(ref _scene,
                                     ref shapeHandle,
                                     ref shapeTransform,
                                     ref instanceHandle);

      return instanceHandle;
    }

    public void UnregisterInteractionObject(InteractionObject interactionObject) {
      _objects.Remove(interactionObject);

      var handle = interactionObject.InstanceHandle;
      InteractionC.LeapIEDestroyShape(ref _scene,
                                      ref handle);
    }

    void FixedUpdate() {
      updateIeRepresentations();

      updateIeTracking();

      simulateIe();

      handleIeEvents();
    }

    private void updateIeRepresentations() {
      foreach (var obj in _objects) {
        var instanceTransform = obj.IeTransform;
        var instanceHandle = obj.InstanceHandle;

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

    private void handleIeEvents() {
      object ie_event;
      while (true) {
        //LeapIEPollEvent(ref ie_event);

        switch (/*ie_event.type*/0) {
          //eLeapIEEventType_None
          case 0:
            return;
          //eLeapIEEventType_ObjectGrabStart
          case 1:
            handleEvent_grabStart();
            break;
          //eLeapIEEventType_ObjectGrabStop
          case 2:
            handleEvent_grabStop();
            break;
          //eLeapIEEventType_ObjectGrabMove
          case 3:
            handleEvent_grabMove();
            break;
          //eLeapIEEventType_ObjectGrabResume
          case 4:
            handleEvent_grabResume();
            break;
          //eLeapIEEventType_ObjectGrabSuspend
          case 5:
            handleEvent_grabSuspend();
            break;
        }
      }
    }

    private void handleEvent_grabStart(/*LEAP_IE_EVENT_OBJECT_GRAB_START grabStartEvent*/) {
      InteractionObject interactionObject = _objectIdMap[/*grabStartEvent.object.id*/0];
      interactionObject.HandleGrabStart(/*grabStartEvent*/null);
    }

    private void handleEvent_grabStop(/*LEAP_IE_EVENT_OBJECT_GRAB_STOP grabStopEvent*/) {
      InteractionObject interactionObject = _objectIdMap[/*grabStopEvent.object.id*/0];
      interactionObject.HandleGrabStop(/*grabStopEvent*/null);
    }

    private void handleEvent_grabMove(/*LEAP_IE_EVENT_OBJECT_GRAB_MOVE grabMoveEvent*/) {
      InteractionObject interactionObject = _objectIdMap[/*grabMoveEvent.object.id*/0];
      interactionObject.HandleGrabMove(/*grabMoveEvent*/null);
    }

    private void handleEvent_grabResume(/*LEAP_IE_EVENT_OBJECT_GRAB_RESUME grabResumeEvent*/) {
      InteractionObject interactionObject = _objectIdMap[/*grabResumeEvent.object.id*/0];
      interactionObject.HandleGrabResume(/*grabResumeEvent*/null);
    }

    private void handleEvent_grabSuspend(/*LEAP_IE_EVENT_OBJECT_GRAB_SUSPEND grabSuspendtEvent*/) {
      InteractionObject interactionObject = _objectIdMap[/*grabSuspendEvent.object.id*/0];
      interactionObject.HandleGrabSuspend(/*grabSuspendEvent*/null);
    }
  }
}
