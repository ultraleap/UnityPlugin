using UnityEngine;
using System.Collections.Generic;

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
      IInteractionBehaviour otherBehaviour;
      if (!tryGetOtherBehaviour(collision, out otherBehaviour)) {
        return;
      }

      handleColliding(otherBehaviour);
    }

    void OnCollisionStay(Collision collision) {
      IInteractionBehaviour otherBehaviour;
      if (!tryGetOtherBehaviour(collision, out otherBehaviour)) {
        return;
      }

      handleColliding(otherBehaviour);
    }

    private void handleColliding(IInteractionBehaviour otherBehaviour) {
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

    private bool tryGetOtherBehaviour(Collision collision, out IInteractionBehaviour behaviour) {
      Rigidbody otherBody = collision.rigidbody;
      if (otherBody == null) {
        behaviour = null;
        return false;
      }

      behaviour = otherBody.GetComponent<IInteractionBehaviour>();
      if (behaviour == null) {
        return false;
      }

      if (!_manager.IsRegistered(behaviour)) {
        behaviour = null;
        return false;
      }

      return true;
    }

#if UNITY_EDITOR
    private List<Collider> _colliderList = new List<Collider>();
    public void OnDrawGizmos() {
      Matrix4x4 currMatrix = Gizmos.matrix;

      if (_interactionBehaviour.IsBeingGrasped) {
        Gizmos.color = Color.green;
      } else if (GetComponent<Rigidbody>().IsSleeping()) {
        Gizmos.color = Color.gray;
      } else {
        Gizmos.color = Color.blue;
      }

      Gizmos.color = Color.HSVToRGB(_life / (_manager.MaxDepth * 2.0f), 1, 1);
      drawObject(gameObject, true);

      Gizmos.matrix = currMatrix;
    }

    private void drawObject(GameObject obj, bool wire) {
      obj.GetComponentsInChildren(_colliderList);
      for (int i = 0; i < _colliderList.Count; i++) {
        Collider collider = _colliderList[i];
        Gizmos.matrix = Matrix4x4.TRS(collider.transform.position, collider.transform.rotation, collider.transform.lossyScale * 1.01f);

        if (collider is BoxCollider) {
          BoxCollider box = collider as BoxCollider;
          if (wire) {
            Gizmos.DrawWireCube(box.center, box.size);
          } else {
            Gizmos.DrawCube(box.center, box.size);
          }
        } else if (collider is SphereCollider) {
          SphereCollider sphere = collider as SphereCollider;
          if (wire) {
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
          } else {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
          }
        } else if (collider is CapsuleCollider) {
          CapsuleCollider capsule = collider as CapsuleCollider;
          Vector3 size = Vector3.zero;
          size += Vector3.one * capsule.radius * 2;
          size += new Vector3(capsule.direction == 0 ? 1 : 0,
                              capsule.direction == 1 ? 1 : 0,
                              capsule.direction == 2 ? 1 : 0) * (capsule.height - capsule.radius * 2);
          if (wire) {
            Gizmos.DrawWireCube(capsule.center, size);
          } else {
            Gizmos.DrawCube(capsule.center, size);
          }
        } else if (collider is MeshCollider) {
          Gizmos.matrix = Matrix4x4.identity;
          MeshCollider mesh = collider as MeshCollider;
          Gizmos.DrawWireCube(mesh.bounds.center, mesh.bounds.size);
        }
      }
    }
#endif
  }
}
