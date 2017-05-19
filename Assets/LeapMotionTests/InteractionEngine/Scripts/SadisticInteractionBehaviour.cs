/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Interaction.Testing {

  public class SadisticInteractionBehaviour : InteractionBehaviour {
    protected override void OnRegistered() {
      base.OnRegistered();
      checkCallback(InteractionCallback.OnRegister);
    }

    protected override void OnUnregistered() {
      base.OnUnregistered();
      checkCallback(InteractionCallback.OnUnregister);
    }

    protected override void OnGraspBegin() {
      base.OnGraspBegin();
      checkCallback(InteractionCallback.OnGrasp);
    }

    protected override void OnGraspEnd(Hand lastHand) {
      base.OnGraspEnd(lastHand);
      checkCallback(InteractionCallback.OnRelease);
    }

    protected override void OnHandLostTracking(Hand oldHand, out float maxSuspensionTime) {
      base.OnHandLostTracking(oldHand, out maxSuspensionTime);
      checkCallback(InteractionCallback.OnSuspend);
    }

    protected override void OnHandRegainedTracking(Hand newHand, int oldId) {
      base.OnHandRegainedTracking(newHand, oldId);
      checkCallback(InteractionCallback.OnResume);
    }

    private float _afterDelayTime;
    protected override void OnEnable() {
      base.OnEnable();
      _afterDelayTime = Time.time + SadisticTest.current.actionDelay;
    }

    void Update() {
      if (Time.time >= _afterDelayTime) {
        _afterDelayTime = float.MaxValue;
        checkCallback(InteractionCallback.AfterDelay);
      }
    }

    private void checkCallback(InteractionCallback callback) {
      SadisticTest.current.ReportCallback(callback);

      Validate();

      if (SadisticTest.current.callback == callback) {
        executeSadisticAction(SadisticTest.current.action);
      }
    }

    private void executeSadisticAction(SadisticAction action) {
      switch (action) {
        case SadisticAction.DisableComponent:
          enabled = false;
          break;
        case SadisticAction.DestroyComponent:
          Destroy(this);
          break;
        case SadisticAction.DestroyComponentImmediately:
          DestroyImmediate(this);
          break;
        case SadisticAction.DisableGameObject:
          gameObject.SetActive(false);
          break;
        case SadisticAction.DestroyGameObject:
          Destroy(gameObject);
          break;
        case SadisticAction.DestroyGameObjectImmediately:
          DestroyImmediate(gameObject);
          break;
        case SadisticAction.ForceGrab:
          Hand hand = FindObjectOfType<LeapProvider>().CurrentFrame.Hands[0];
          _manager.GraspWithHand(hand, this);
          break;
        case SadisticAction.ForceRelease:
          _manager.ReleaseObject(this);
          break;
        case SadisticAction.DisableGrasping:
          _manager.GraspingEnabled = false;
          break;
        case SadisticAction.DisableContact:
          _manager.ContactEnabled = false;
          break;
        default:
          break;
      }
    }
  }
}
