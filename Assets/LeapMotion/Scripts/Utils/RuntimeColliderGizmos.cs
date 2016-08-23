using UnityEngine;

namespace Leap.Unity.RuntimeGizmos {

  public class RuntimeColliderGizmos : MonoBehaviour, IRuntimeGizmoComponent {

    public Color color = Color.white;
    public bool useWireframe = true;
    public bool traverseHierarchy = true;

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      drawer.color = color;
      drawer.DrawColliders(gameObject, useWireframe, traverseHierarchy);
    }
  }
}
