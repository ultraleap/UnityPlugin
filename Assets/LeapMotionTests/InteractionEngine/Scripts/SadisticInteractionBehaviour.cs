using UnityEngine;
using System;
using System.Collections.Generic;
using Leap.Unity.Interaction.CApi;
using Leap.Unity.Attributes;

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
      _afterDelayTime = Time.time + SadisticTest.currentDefinition.delay;
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


      if (SadisticTest.currentDefinition.callback == callback) {
        executeSadisticAction(SadisticTest.currentDefinition.action);
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

    public enum SadisticAction {
      DisableComponent = 0x0001,
      DestroyComponent = 0x0002,
      DestroyComponentImmediately = 0x0004,
      DisableGameObject = 0x0008,
      DestroyGameObject = 0x0010,
      DestroyGameObjectImmediately = 0x0020,
      ForceGrab = 0x0040,
      ForceRelease = 0x0080
    }

    public enum Callback {
      OnRegister = 0x0001,
      OnUnregister = 0x0002,
      OnCreateInstance = 0x0004,
      OnDestroyInstance = 0x0008,
      OnGrasp = 0x0010,
      OnRelease = 0x0020,
      OnSuspend = 0x0040,
      OnResume = 0x0080,
      AfterDelay = 0x0100,
    }

    [Serializable]
    public class SadisticDef {
      public Callback callback;
      [EnumFlags]
      public Callback expectedCallbacks;
      public SadisticAction action;
      public float delay;

      public SadisticDef(Callback callback, Callback expectedCallbacks, SadisticAction action, float delay) {
        this.callback = callback;
        this.expectedCallbacks = expectedCallbacks;
        this.action = action;
        this.delay = delay;
      }
    }
  }
}
