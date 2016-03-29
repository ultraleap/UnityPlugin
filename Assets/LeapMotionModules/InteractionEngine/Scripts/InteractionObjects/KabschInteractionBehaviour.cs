using LeapInternal;
using Leap.Unity.Interaction.CApi;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  public class KabschInteractionBehaviour : InteractionBehaviour {

    protected Rigidbody _rigidbody;
    protected LEAP_IE_KABSCH _kabsch;

    #region INTERACTION CALLBACKS
    public override void OnPush(Vector3 linearVelocity, Vector3 angularVelocity) {
      base.OnPush(linearVelocity, angularVelocity);

      if (_rigidbody != null) {
        _rigidbody.velocity = linearVelocity;
        _rigidbody.angularVelocity = angularVelocity;
      }
    }

    public override void OnHandGrasp(Hand hand) {
      base.OnHandGrasp(hand);
    }

    public override void OnHandsHold(List<Hand> hands) {
      base.OnHandsHold(hands);
    }

    public override void OnHandRelease(Hand hand) {
      base.OnHandRelease(hand);
    }

    public override void OnHandLostTracking(Hand oldHand) {
      base.OnHandLostTracking(oldHand);
    }

    public override void OnHandRegainedTracking(Hand newHand, int oldId) {
      base.OnHandRegainedTracking(newHand, oldId);
    }

    public override void OnHandTimeout(Hand oldHand) {
      base.OnHandTimeout(oldHand);
    }

    protected override void OnGraspBegin() {
      base.OnGraspBegin();
    }

    protected override void OnGraspEnd() {
      base.OnGraspEnd();
    }

    #endregion

    #region UNITY CALLBACKS
    protected virtual void Awake() {
      KabschC.Construct(ref _kabsch);
    }

    protected virtual void OnEnable() {
      EnableInteraction();
    }

    protected virtual void Start() {
      _rigidbody = GetComponent<Rigidbody>();
    }

    protected virtual void OnDisable() {
      DisableInteraction();
    }

    protected virtual void OnDestroy() {
      KabschC.Destruct(ref _kabsch);
    }
    #endregion




  }

}
