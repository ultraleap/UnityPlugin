/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using Leap.Unity.RuntimeGizmos;

namespace Leap.Unity.Interaction {

  public abstract class IActivityMonitor : MonoBehaviour {
    public static GizmoType gizmoType = GizmoType.InteractionStatus;
    public static float explosionVelocity = 100;                   //In meters per second
    public static int hysteresisTimeout = 5;                       //In fixed frames

    public abstract void Init(IInteractionBehaviour interactionBehaviour, ActivityManager manager);
    public abstract void Revive();
    public abstract void UpdateState();

    public int arrayIndex;

    public enum GizmoType {
      InteractionStatus,
      ActivityDepth
    }
  }

  public class ActivityMonitorLite : IActivityMonitor, IRuntimeGizmoComponent {
    protected Rigidbody _rigidbody;
    protected IInteractionBehaviour _interactionBehaviour;
    protected ActivityManager _manager;

    // For explosion protection
    protected Vector3 _prevPosition;
    protected Quaternion _prevRotation;
    protected Vector3 _prevVelocity;
    protected Vector3 _prevAngularVelocity;

    protected int _timeToLive = 1;
    protected int _timeToDie = 0;  // Timer after _timeToLive goes negative before deactivation.

    public override void Init(IInteractionBehaviour interactionBehaviour, ActivityManager manager) {
      _interactionBehaviour = interactionBehaviour;
      _manager = manager;
      Revive();

      _rigidbody = GetComponent<Rigidbody>();

      _prevPosition = _rigidbody.position;
      _prevVelocity = _rigidbody.velocity;
      _prevRotation = _rigidbody.rotation;
      _prevAngularVelocity = _rigidbody.angularVelocity;
    }

    public override void Revive() {
      _timeToLive = 1;
    }

    public override void UpdateState() {
      if (!_rigidbody.isKinematic) {
        bool didExplode = (_rigidbody.position - _prevPosition).sqrMagnitude / Time.fixedDeltaTime >= explosionVelocity * explosionVelocity;

        if (_interactionBehaviour is InteractionBehaviour) {
          if ((_interactionBehaviour as InteractionBehaviour).WasTeleported) {
            didExplode = false;
          }
        }

        if (didExplode) {
          Debug.LogWarning("Explosion was detected!  Object " + gameObject + " has been reset to its previous state.  If this was " +
                           "intentional movement, make sure you have called NotifyTeleported on the InteractionBehaviour, or raise " +
                           "the explosion velocity threshold.");

          _rigidbody.velocity = _prevVelocity;
          _rigidbody.angularVelocity = _prevAngularVelocity;
          _rigidbody.position = _prevPosition + _rigidbody.velocity * Time.fixedDeltaTime;
          _rigidbody.rotation = _prevRotation;
        }
      }

      _prevPosition = _rigidbody.position;
      _prevRotation = _rigidbody.rotation;
      _prevVelocity = _rigidbody.velocity;
      _prevAngularVelocity = _rigidbody.angularVelocity;

      // Grasped objects do not intersect the brush layer but are still touching hands.
      if (_interactionBehaviour.IsBeingGrasped) {
        Revive();
        return;
      }

      if (_timeToLive > 0) {
        --_timeToLive;
        _timeToDie = 0;
      } else {
        if (_interactionBehaviour.IsAbleToBeDeactivated() && ++_timeToDie >= hysteresisTimeout) {
          _manager.Deactivate(_interactionBehaviour);
        }
      }
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      switch (gizmoType) {
        case GizmoType.InteractionStatus:
          if (_interactionBehaviour.IsBeingGrasped) {
            drawer.color = Color.green;
          } else if (GetComponent<Rigidbody>().IsSleeping()) {
            drawer.color = Color.gray;
          } else {
            drawer.color = Color.blue;
          }
          break;
        case GizmoType.ActivityDepth:
          drawer.color = Color.HSVToRGB(Mathf.Max(0, _timeToLive) / (_manager.MaxDepth * 2.0f), 1, 1);
          break;
      }

      drawer.DrawColliders(gameObject);
    }
  }

  public class ActivityMonitor : ActivityMonitorLite {

    public override void Init(IInteractionBehaviour interactionBehaviour, ActivityManager manager) {
      base.Init(interactionBehaviour, manager);

      bool wasSleeping = _rigidbody.IsSleeping();

      //We need to do this in order to force Unity to reconsider collision callbacks for this object
      //Otherwise scripts added in the middle of a collision never recieve the Stay callbacks.
      Collider singleCollider = GetComponentInChildren<Collider>();
      if (singleCollider != null) {
        Physics.IgnoreCollision(singleCollider, singleCollider, true);
        Physics.IgnoreCollision(singleCollider, singleCollider, false);
      }

      if (wasSleeping) {
        _rigidbody.Sleep();
      }
    }

    void OnCollisionEnter(Collision collision) {
      handleCollision(collision);
    }

    void OnCollisionStay(Collision collision) {
      handleCollision(collision);
    }

    private void handleCollision(Collision collision) {
      IInteractionBehaviour otherBehaviour = null;
      ActivityMonitor neighbor = collision.gameObject.GetComponent<ActivityMonitor>();
      if (neighbor != null) {
        if (arrayIndex > neighbor.arrayIndex) {
          return; // Only need to do this on one side of a pair.
        }

        otherBehaviour = neighbor._interactionBehaviour;
      } else {
        if (_timeToLive <= 1) {
          return; // Do not activate neighbor.
        }

        otherBehaviour = collision.gameObject.GetComponent<IInteractionBehaviour>();
        if (otherBehaviour == null) {
          return;
        }

        // Unregistered behaviours will fail to activate.
        neighbor = _manager.Activate(otherBehaviour) as ActivityMonitor;
        if (neighbor != null) {
          neighbor._timeToLive = _timeToLive - 1;
        }
        return;
      }

      // Allow different managers.
      if (!_manager.IsRegistered(otherBehaviour)) {
        return;
      }

      // propagate both ways
      int nextTime = ((_timeToLive > neighbor._timeToLive) ? _timeToLive : neighbor._timeToLive) - 1;
      if (_timeToLive < nextTime) {
        _timeToLive = nextTime;
      } else if (neighbor._timeToLive < nextTime) {
        neighbor._timeToLive = nextTime;
      }
    }
  }
}
