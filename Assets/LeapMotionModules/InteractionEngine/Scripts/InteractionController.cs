using UnityEngine;
using Leap;
using InteractionEngine.Internal;

namespace InteractionEngine {

  public class InteractionController : MonoBehaviour {
    [SerializeField]
    private LeapProvider _leapProvider;

    private LEAP_IT_SCENE _scene;

    private OneToOneMap<InteractionObject, LEAP_IE_SHAPE_INSTANCE_HANDLE> _objectIdMap = new OneToOneMap<InteractionObject, LEAP_IE_SHAPE_INSTANCE_HANDLE>();

    public void RegisterInteractionObject(InteractionObject interactionObject) {
      _objectIdMap.Add(interactionObject, interactionObject.Handle);

      InteractionC.LeapIEAddShapeDescription(0, interactionObject.ShapeDescription, interactionObject.Handle);
      //LeapIEAddInteractionObject(representation);
    }

    public void UnregisterInteractionObject(InteractionObject interactionObject) {
      _objectIdMap.Remove(interactionObject);

      object representation = interactionObject.GetRepresentation();
      //LeapIERemoveInteractionObject(representation);
    }

    void FixedUpdate() {
      updateIeRepresentations();

      updateIeTracking();

      simulateIe();

      handleIeEvents();
    }

    private void updateIeRepresentations() {
      foreach (var interactionObjects in _objectIdMap.Keys) {
        object representation = interactionObjects.GetRepresentation();
        //LeapIEUpdateInteractionObject(representation);
      }
    }

    private void updateIeTracking() {
      Frame frame = _leapProvider.GetFixedFrame();
      //LeapIEUpdateLeapTracking(frame);
    }

    private void simulateIe() {
      //LeapIESimulate(Time.fixedDeltaTime, UnityMatrixExtension.GetLeapMatrix(_leapProvider.transform));
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
