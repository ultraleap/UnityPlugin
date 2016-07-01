using UnityEngine;

namespace Leap.Unity.Interaction {

  public class ActivityMonitor : MonoBehaviour {
    public const int DISABLE_HYSTERESIS = 5;

    public enum GizmoType {
      InteractionStatus,
      ActivityDepth
    }

    public static GizmoType gizmoType = GizmoType.InteractionStatus;

    private IInteractionBehaviour _interactionBehaviour;
    private ActivityManager _manager;
    private int _life;
    private int _maxNeighborLife;

    public void Init(IInteractionBehaviour interactionBehaviour, ActivityManager manager) {
      _interactionBehaviour = interactionBehaviour;
      _manager = manager;
      Revive();
    }

    public void Revive() {
      _maxNeighborLife = _manager.MaxDepth;
      _life = _manager.MaxDepth;
    }

    void FixedUpdate() {
      if (_life >= _maxNeighborLife) {
        _life--;
      }

      if (_life < -DISABLE_HYSTERESIS) {
        if (_interactionBehaviour.IsBeingGrasped || _interactionBehaviour.UntrackedHandCount > 0) {
          _life = 1;
        } else {
          _manager.Deactivate(_interactionBehaviour);
        }
      }

      _maxNeighborLife = int.MinValue;
    }

    void OnCollisionEnter(Collision collision) {
      handleCollision(collision);
    }

    void OnCollisionStay(Collision collision) {
      handleCollision(collision);
    }

    private void handleCollision(Collision collision) {
      if (collision.rigidbody == null) {
        return;
      }

      IInteractionBehaviour otherBehaviour = collision.rigidbody.GetComponent<IInteractionBehaviour>();
      if (otherBehaviour == null) {
        return;
      }

      if (!_manager.IsRegistered(otherBehaviour)) {
        return;
      }

      ActivityMonitor neighbor = otherBehaviour.GetComponent<ActivityMonitor>();
      if (neighbor == null) {
        if (_life > 1) {
          neighbor = _manager.Activate(otherBehaviour);
          neighbor._life = _life - 1;
        }
      } else {
        _maxNeighborLife = Mathf.Max(_maxNeighborLife, neighbor._life);
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
          Gizmos.color = Color.HSVToRGB(Mathf.Max(0, _life) / (_manager.MaxDepth * 2.0f), 1, 1);
          break;
      }

      GizmoUtility.DrawColliders(gameObject, useWireframe: true);
    }
#endif
  }
}
