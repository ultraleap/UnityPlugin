using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Examples {

  public class ThrottleHandle : MonoBehaviour {

    public Throttle throttle;

    private float _throttleLength = 0.27F;
    private Vector3 _throttleDirection = Vector3.forward;
    private Vector3 _localThrottleStartPos;
    private Vector3 _localThrottleEndPos;
    private float _fractionAlongSegment = 0F;

    private Vector3 _baseScaleVector;
    private float _contactScale = 1.3F;

    private bool _contactingHand;
    private bool _beingHeld;

    private InteractionBehaviour _interactionObj;

    void Start() {
      _localThrottleStartPos = this.transform.localPosition;
      _localThrottleEndPos   = this.transform.localPosition + (this.transform.localRotation * _throttleDirection) * _throttleLength;
      _baseScaleVector = this.transform.localScale;

      _interactionObj = GetComponent<InteractionBehaviour>();
      _interactionObj.OnObjectContactBegin += OnObjectContactBegin;
      _interactionObj.OnObjectContactEnd   += OnObjectContactEnd;
      _interactionObj.OnGraspBegin         += OnGraspBegin;
      _interactionObj.OnGraspEnd           += OnGraspEnd;
      _interactionObj.OnPreGraspedMovement += OnPreGraspedMovement;
      _interactionObj.OnGraspedMovement    += OnGraspedMovement;
    }

    private void OnObjectContactBegin(Hand hand) {
      _contactingHand = true;
    }

    private void OnObjectContactEnd(Hand hand) {
      _contactingHand = false;
    }

    private void OnGraspBegin(Hand hand) {
      _beingHeld = true;
    }

    private void OnGraspEnd(Hand hand) {
      _beingHeld = false;
    }

    Quaternion _origRot;
    private void OnPreGraspedMovement(Vector3 pos, Quaternion rot, Hand hand) {
      _origRot = rot;
    }

    private void OnGraspedMovement(Vector3 newPos, Quaternion newRot, Hand hand) {
      Vector3 handlePos = ConstraintsUtil.ConstrainToLineSegment(newPos,
        this.transform.parent.TransformPoint(_localThrottleStartPos),
        this.transform.parent.TransformPoint(_localThrottleEndPos),
        out _fractionAlongSegment);

      throttle.SetThrottleAmount(_fractionAlongSegment);

      this.transform.position = handlePos;
      _interactionObj.Rigidbody.position = handlePos;

      this.transform.rotation = _origRot;
      _interactionObj.Rigidbody.rotation = _origRot;
    }

    void Update() {
      Vector3 targetScaleVector = _baseScaleVector * (_contactingHand ? _contactScale : 1F);

      this.transform.localScale = Vector3.Lerp(this.transform.localScale, targetScaleVector, 20F * Time.deltaTime);
    }

  }


}