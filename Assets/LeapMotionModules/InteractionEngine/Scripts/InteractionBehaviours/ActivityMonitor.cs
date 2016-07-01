using UnityEngine;

namespace Leap.Unity.Interaction {

  public class ActivityMonitor : MonoBehaviour {
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
      _maxNeighborLife = _manager.MaxLife;
      _life = _manager.MaxLife;
    }

    void FixedUpdate() {
      if (_life >= _maxNeighborLife) {
        _life--;
      }

      if (_life <= 0) {
        if (_interactionBehaviour.IsBeingGrasped || _interactionBehaviour.UntrackedHandCount > 0) {
          _life = 1;
        } else {
          _manager.Deactivate(_interactionBehaviour);
        }
      }

      _maxNeighborLife = 0;
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
        if (_life > _manager.LifeStep) {
          neighbor = _manager.Activate(otherBehaviour);
          neighbor._life = _life - _manager.LifeStep;
        }
      } else {
        _maxNeighborLife = Mathf.Max(_maxNeighborLife, neighbor._life);
      }
    }

#if UNITY_EDITOR
    public void OnDrawGizmos() {
      if (_interactionBehaviour.IsBeingGrasped) {
        Gizmos.color = Color.green;
      } else if (GetComponent<Rigidbody>().IsSleeping()) {
        Gizmos.color = Color.gray;
      } else {
        Gizmos.color = Color.blue;
      }

      Gizmos.color = Color.HSVToRGB(_life / (_manager.MaxLife * 2.0f), 1, 1);
      GizmoUtility.DrawColliders(gameObject, useWireframe: true);
    }
#endif
  }
}
