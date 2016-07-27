using UnityEngine;
using System;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction.Testing {

  public class SadisticInteractionBehaviour : InteractionBehaviour {
    protected override void OnRegistered() {
      base.OnRegistered();
      checkCallback(Callback.OnRegister);
    }

    protected override void OnUnregistered() {
      base.OnUnregistered();
      checkCallback(Callback.OnUnregister);
    }

    protected override void OnInteractionShapeCreated(INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle) {
      base.OnInteractionShapeCreated(instanceHandle);
      checkCallback(Callback.OnCreateInstance);
    }

    protected override void OnInteractionShapeDestroyed() {
      base.OnInteractionShapeDestroyed();
      checkCallback(Callback.OnDestroyInstance);
    }

    protected override void OnGraspBegin() {
      base.OnGraspBegin();
      checkCallback(Callback.OnGrasp);
    }

    protected override void OnGraspEnd(Hand lastHand) {
      base.OnGraspEnd(lastHand);
      checkCallback(Callback.OnRelease);
    }

    protected override void OnHandLostTracking(Hand oldHand, out float maxSuspensionTime) {
      base.OnHandLostTracking(oldHand, out maxSuspensionTime);
      checkCallback(Callback.OnSuspend);
    }

    protected override void OnHandRegainedTracking(Hand newHand, int oldId) {
      base.OnHandRegainedTracking(newHand, oldId);
      checkCallback(Callback.OnResume);
    }

    private float _afterDelayTime;
    protected override void OnEnable() {
      base.OnEnable();
      _afterDelayTime = Time.time + SadisticTest.current.delay;
    }

    void Update() {
      if (Time.time >= _afterDelayTime) {
        checkCallback(Callback.AfterDelay);
        _afterDelayTime = float.MaxValue;
      }
    }

    private void checkCallback(Callback callback) {
      SadisticTest.allCallbacksRecieved |= callback;

      try {
        Validate();
      } catch (Exception e) {
        Debug.LogException(e);
        IntegrationTest.Fail("Validation failed during callback " + callback + "\n" + e.Message);
      }


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
        default:
          break;
      }
    }
  }
}
