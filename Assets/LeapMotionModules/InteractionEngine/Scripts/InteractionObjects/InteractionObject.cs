using UnityEngine;
using System;
using LeapInternal;
using InteractionEngine.Internal;

namespace InteractionEngine {

  public abstract class InteractionObject : MonoBehaviour {
    private static uint _nextHandle = 1;

    [SerializeField]
    private InteractionController _controller;

    public event Action OnGrabStart;
    public event Action OnGrabMove;
    public event Action OnGrabStop;
    public event Action OnGrabSuspend;
    public event Action OnGrabResume;

    private LEAP_IE_SHAPE_INSTANCE_HANDLE _ieInstanceHandle;
    private object _ieShapeDescription;

    public LEAP_IE_SHAPE_INSTANCE_HANDLE Handle {
      get {
        return _ieInstanceHandle;
      }
    }

    public object ShapeDescription {
      get {
        LEAP_IE_SPHERE_DESCRIPTION sphereDescription = new LEAP_IE_SPHERE_DESCRIPTION();
        sphereDescription.shape.type = eLeapIEShapeType.eLeapIERS_ShapeSphere;
        sphereDescription.radius = 0.1f;
        return sphereDescription;
      }
    }

    public LEAP_IE_TRANSFORM IeTransform {
      get {
        LEAP_IE_TRANSFORM ieTransform = new LEAP_IE_TRANSFORM();
        ieTransform.position = new LEAP_VECTOR(transform.position);
        ieTransform.rotation = new LEAP_QUATERNION(transform.rotation);
        return ieTransform;
      }
      set {
        transform.position = value.position.ToUnityVector();
        transform.rotation = value.rotation.ToUnityRotation();
      }
    }

    public virtual void HandleGrabStart(/*LEAP_IE_EVENT_OBJECT_GRAB_START grabStartEvent*/ object eventObj) {
      if (OnGrabStart != null) {
        OnGrabStart();
      }
    }

    public virtual void HandleGrabStop(/*LEAP_IE_EVENT_OBJECT_GRAB_STOP grabStopEvent*/ object eventObj) {
      if (OnGrabStop != null) {
        OnGrabStop();
      }
    }

    public virtual void HandleGrabMove(/*LEAP_IE_EVENT_OBJECT_GRAB_MOVE grabMoveEvent*/ object eventObj) {
      if (OnGrabMove != null) {
        OnGrabMove();
      }
    }

    public virtual void HandleGrabResume(/*LEAP_IE_EVENT_OBJECT_GRAB_RESUME grabResumeEvent*/ object eventObj) {
      if (OnGrabResume != null) {
        OnGrabResume();
      }
    }

    public virtual void HandleGrabSuspend(/*LEAP_IE_EVENT_OBJECT_GRAB_SUSPEND grabSuspendEvent*/ object eventObj) {
      if (OnGrabSuspend != null) {
        OnGrabSuspend();
      }
    }

    protected virtual void Awake() {
      _ieInstanceHandle.handle = _nextHandle;
      _nextHandle++;
    }

    protected virtual void OnValidate() {
      if (_controller == null) {
        _controller = FindObjectOfType<InteractionController>();
      }
    }

    protected virtual void OnEnable() {
      _controller.RegisterInteractionObject(this);
    }

    protected virtual void OnDisable() {
      _controller.UnregisterInteractionObject(this);
    }
  }
}
