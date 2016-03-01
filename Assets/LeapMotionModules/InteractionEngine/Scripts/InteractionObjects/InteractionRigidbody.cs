using UnityEngine;
using System.Collections;

namespace InteractionEngine {

  public class InteractionRigidbody : InteractionObject {

    [SerializeField]
    private bool _ungrabbedKinematicState = false;

    private Rigidbody _rigidbody;

    public bool UngrabbedKinematicState {
      get {
        return _ungrabbedKinematicState;
      }
      set {
        _ungrabbedKinematicState = value;
      }
    }

    public override void HandleGrabStart(object eventObj) {
      base.HandleGrabStart(eventObj);
      _rigidbody.isKinematic = true;
    }

    public override void HandleGrabMove(object eventObj) {
      base.HandleGrabMove(eventObj);
      /*
      _rigidbody.MovePosition(eventObj.object.position);
      _rigidbody.MoveRotation(eventObj.object.rotation);
      */
    }

    public override void HandleGrabStop(object eventObj) {
      base.HandleGrabStop(eventObj);
      _rigidbody.isKinematic = _ungrabbedKinematicState;
    }

    protected override void OnValidate() {
      base.OnValidate();
      if (!Application.isPlaying) {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody != null) {
          _rigidbody.isKinematic = _ungrabbedKinematicState;
        }
      }
    }

    protected virtual void Start() {
      _rigidbody = GetComponent<Rigidbody>();
    }

  }
}
