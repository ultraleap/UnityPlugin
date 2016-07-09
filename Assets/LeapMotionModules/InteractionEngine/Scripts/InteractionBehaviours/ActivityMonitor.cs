using UnityEngine;

namespace Leap.Unity.Interaction {

  public class ActivityMonitor : MonoBehaviour {
    private const int HYSTERESIS_TIMEOUT = 5;

    public enum GizmoType {
      InteractionStatus,
      ActivityDepth
    }

    public static GizmoType gizmoType = GizmoType.ActivityDepth;

    // Caches index into ActivityManager array.
    public int arrayIndex = -1;

    private IInteractionBehaviour _interactionBehaviour;
    private ActivityManager _manager;
    private int _timeToLive = 0; // Converges to remaining allowed distance to hands across contact graph.
    private int _timeToDie = 0;  // Timer after _timeToLive goes negative before deactivation.

    public void Init(IInteractionBehaviour interactionBehaviour, ActivityManager manager) {
      _interactionBehaviour = interactionBehaviour;
      _manager = manager;
      Revive();
    }

    public void Revive() {
      // This has a contact graph distance of 0 from the hands.
      _timeToLive = _manager.MaxDepth;
    }

    void FixedUpdate() {
      // TODO: Is this the right place for this check, or is it already implicit?
      if (_interactionBehaviour.IsBeingGrasped || _interactionBehaviour.UntrackedHandCount > 0) {
        Revive();
        return;
      }

      if (_timeToLive > 0) {
        --_timeToLive;
        _timeToDie = 0;
      } else if(++_timeToDie >= HYSTERESIS_TIMEOUT) {
        _manager.Deactivate(_interactionBehaviour); //
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
      }
      else {
        if(_timeToLive <= 1) {
          return; // Do not activate neighbor.
        }

        otherBehaviour = collision.gameObject.GetComponent<IInteractionBehaviour>();
        if (otherBehaviour == null) {
          return;
        }

        neighbor = _manager.Activate(otherBehaviour);
        neighbor._timeToLive = _timeToLive - 1;
        return;
      }

      // Allow different managers.
      if (!_manager.IsRegistered(otherBehaviour)) {
        return;
      }

      // propagate both ways
      int nextTime = ((_timeToLive > neighbor._timeToLive) ? _timeToLive : neighbor._timeToLive) - 1;
      if(_timeToLive < nextTime) {
        _timeToLive = nextTime;
      }
      else if(neighbor._timeToLive < nextTime) {
        neighbor._timeToLive = nextTime;
      }
    }

#if UNITY_EDITOR
    public void OnDrawGizmos() {
      switch (gizmoType) {
        case GizmoType.InteractionStatus:
          if (_interactionBehaviour.IsBeingGrasped) {
            Gizmos.color = Color.green;
          } else if (GetComponent<Rigidbody>().IsSleeping()) {
            Gizmos.color = Color.gray;
          } else {
            Gizmos.color = Color.blue;
          }
          break;
        case GizmoType.ActivityDepth:
          Gizmos.color = Color.HSVToRGB(Mathf.Max(0, _timeToLive) / (_manager.MaxDepth * 2.0f), 1, 1);
          break;
      }

      GizmoUtility.DrawColliders(gameObject, useWireframe: true);
    }
#endif
  }
}
