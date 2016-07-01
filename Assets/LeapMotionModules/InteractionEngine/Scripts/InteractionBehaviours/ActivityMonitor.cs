using UnityEngine;

namespace Leap.Unity.Interaction {

  public class ActivityMonitor : MonoBehaviour {
    private IInteractionBehaviour _interactionBehaviour;
    private ActiveObjectManager _manager;
    private int _life;

    public void Init(IInteractionBehaviour interactionBehaviour, ActiveObjectManager manager) {
      _interactionBehaviour = interactionBehaviour;
      _manager = manager;
      Revive();
    }

    public void Revive() {
      _life = _manager.MaxDepth;
    }

    void FixedUpdate() {
      if (_life <= 0) {
        if (_interactionBehaviour.IsBeingGrasped || _interactionBehaviour.UntrackedHandCount > 0) {
          _life = 1;
        } else {
          _manager.Deactivate(_interactionBehaviour);
        }
      }

      //Very important to decrement after the check
      //If we decremented before, an object that just collided and set its life to 1 would be deactivated
      //And would very likely be re-activated the very next frame due to another collision.
      _life--;
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
        _life = Mathf.Max(_life, neighbor._life - 1);
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

      Gizmos.color = Color.HSVToRGB(_life / (_manager.MaxDepth * 2.0f), 1, 1);
      GizmoUtility.DrawColliders(gameObject, useWireframe: true);
    }
#endif
  }
}
