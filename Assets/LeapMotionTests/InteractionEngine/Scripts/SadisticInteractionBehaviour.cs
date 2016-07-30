using UnityEngine;
using Leap.Unity.Interaction.CApi;

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

    protected override void OnInteractionShapeCreated(INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle) {
      base.OnInteractionShapeCreated(instanceHandle);
      checkCallback(InteractionCallback.OnCreateInstance);
    }

    protected override void OnInteractionShapeDestroyed() {
      base.OnInteractionShapeDestroyed();
      checkCallback(InteractionCallback.OnDestroyInstance);
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

    protected override void OnRecievedSimulationResults(INTERACTION_SHAPE_INSTANCE_RESULTS results) {
      base.OnRecievedSimulationResults(results);

      if ((results.resultFlags & ShapeInstanceResultFlags.Velocities) != 0) {
        checkCallback(InteractionCallback.RecieveVelocityResults);
      }
    }

    private float _afterDelayTime;
    protected override void OnEnable() {
      base.OnEnable();
      _afterDelayTime = Time.time + SadisticTest.current.actionDelay;
    }

    void Update() {
      if (Time.time >= _afterDelayTime) {
        checkCallback(InteractionCallback.AfterDelay);
        _afterDelayTime = float.MaxValue;
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
        default:
          break;
      }
    }
  }
}
