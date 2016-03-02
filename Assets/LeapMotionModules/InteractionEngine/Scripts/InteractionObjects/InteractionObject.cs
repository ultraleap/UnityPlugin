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

    private LEAP_IE_SHAPE_INSTANCE_HANDLE _instanceHandle;
    private LEAP_IE_SHAPE_DESCRIPTION_HANDLE _shapeDescriptionHandle;
    private object _ieShapeDescription;

    public LEAP_IE_SHAPE_INSTANCE_HANDLE InstanceHandle {
      get {
        return _instanceHandle;
      }
    }

    public LEAP_IE_SHAPE_DESCRIPTION_HANDLE ShapeDescriptionHandle {
      get {
        return _shapeDescriptionHandle;
      }
    }

    public object ShapeDescription {
      get {
        return _ieShapeDescription;
      }
    }

    public LEAP_IE_TRANSFORM IeTransform {
      get {
        LEAP_IE_TRANSFORM ieTransform = new LEAP_IE_TRANSFORM();
        ieTransform.position = new LEAP_VECTOR(transform.position);
        ieTransform.rotation = new LEAP_QUATERNION(transform.rotation);
        ieTransform.wallTime = Time.fixedTime;
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
      LEAP_IE_SPHERE_DESCRIPTION desc = new LEAP_IE_SPHERE_DESCRIPTION();
      desc.shape.type = eLeapIEShapeType.eLeapIERS_ShapeSphere;
      desc.radius = GetComponent<SphereCollider>().radius;
      _ieShapeDescription = desc;

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
