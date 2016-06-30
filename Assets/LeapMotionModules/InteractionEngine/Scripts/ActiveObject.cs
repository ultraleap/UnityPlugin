using UnityEngine;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {

  public class ActiveObject : MonoBehaviour {
    public IInteractionBehaviour interactionBehaviour;
    public int updateIndex = -1;

#if UNITY_EDITOR
    private List<Collider> _colliderList = new List<Collider>();
    public void OnDrawGizmos() {
      Matrix4x4 currMatrix = Gizmos.matrix;

      GetComponentsInChildren(_colliderList);

      if (interactionBehaviour.IsBeingGrasped) {
        Gizmos.color = Color.green;
      } else if (GetComponent<Rigidbody>().IsSleeping()) {
        Gizmos.color = Color.gray;
      } else {
        Gizmos.color = Color.blue;
      }

      for (int i = 0; i < _colliderList.Count; i++) {
        Collider collider = _colliderList[i];
        Gizmos.matrix = Matrix4x4.TRS(collider.transform.position, collider.transform.rotation, collider.transform.lossyScale);

        if (collider is BoxCollider) {
          BoxCollider box = collider as BoxCollider;
          Gizmos.DrawWireCube(box.center, box.size);
        } else if (collider is SphereCollider) {
          SphereCollider sphere = collider as SphereCollider;
          Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        } else if (collider is CapsuleCollider) {
          CapsuleCollider capsule = collider as CapsuleCollider;
          Vector3 size = Vector3.zero;
          size += Vector3.one * capsule.radius * 2;
          size += new Vector3(capsule.direction == 0 ? 1 : 0,
                              capsule.direction == 1 ? 1 : 0,
                              capsule.direction == 2 ? 1 : 0) * (capsule.height - capsule.radius * 2);
          Gizmos.DrawWireCube(capsule.center, size);
        } else if (collider is MeshCollider) {
          Gizmos.matrix = Matrix4x4.identity;
          MeshCollider mesh = collider as MeshCollider;
          Gizmos.DrawWireCube(mesh.bounds.center, mesh.bounds.size);
        }
      }

      Gizmos.matrix = currMatrix;
    }
#endif
  }
}
