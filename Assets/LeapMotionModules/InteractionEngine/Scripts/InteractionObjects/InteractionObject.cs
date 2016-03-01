using UnityEngine;
using System;

namespace InteractionEngine {

  public abstract class InteractionObject : MonoBehaviour {
    private static uint _nextId = 1;

    [SerializeField]
    private InteractionController _controller;

    private uint _id;

    public event Action OnGrabStart;
    public event Action OnGrabMove;
    public event Action OnGrabStop;
    public event Action OnGrabSuspend;
    public event Action OnGrabResume;

    public uint Id {
      get {
        return _id;
      }
    }

    public object GetRepresentation() {
      return null;
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
      _id = _nextId;
      _nextId++;
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
