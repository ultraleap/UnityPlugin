using UnityEngine;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {

  public static class GizmoUtility {
    private static Stack<Matrix4x4> _matrixStack = new Stack<Matrix4x4>();
    private static List<Collider> _colliderList = new List<Collider>();

    public static void PushMatrix() {
      _matrixStack.Push(Gizmos.matrix);
    }

    public static void PopMatrix() {
      Gizmos.matrix = _matrixStack.Pop();
    }

    public static void RelativeTo(Transform transform) {
      Gizmos.matrix = transform.localToWorldMatrix;
    }

    public static void DrawColliders(GameObject gameObject, bool useWireframe = true, bool traverseHierarchy = true) {
      PushMatrix();

      if (traverseHierarchy) {
        gameObject.GetComponentsInChildren(_colliderList);
      } else {
        gameObject.GetComponents(_colliderList);
      }

      for (int i = 0; i < _colliderList.Count; i++) {
        Collider collider = _colliderList[i];
        RelativeTo(collider.transform);

        if (collider is BoxCollider) {
          BoxCollider box = collider as BoxCollider;
          if (useWireframe) {
            Gizmos.DrawWireCube(box.center, box.size);
          } else {
            Gizmos.DrawCube(box.center, box.size);
          }
        } else if (collider is SphereCollider) {
          SphereCollider sphere = collider as SphereCollider;
          if (useWireframe) {
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
          if (useWireframe) {
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

      PopMatrix();
    }
  }
}
