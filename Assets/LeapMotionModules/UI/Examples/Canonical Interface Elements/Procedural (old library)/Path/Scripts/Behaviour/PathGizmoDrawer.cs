using UnityEngine;
using Leap.Unity.Attributes;
using Leap.Unity.RuntimeGizmos;

namespace Procedural.DynamicPath {

  public class PathGizmoDrawer : MonoBehaviour, IRuntimeGizmoComponent {

    [SerializeField]
    private Color _gizmoColor = Color.white;

    [MinValue(2)]
    [SerializeField]
    private int _gizmoSteps = 20;

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      PathBehaviourBase pathBehaviour = GetComponent<PathBehaviourBase>();
      if (pathBehaviour == null) {
        return;
      }

      IPath path = pathBehaviour.Path;
      drawer.color = _gizmoColor;

      float length = path.Length;
      Vector3 prev = Vector3.zero;
      for (int i = 0; i < _gizmoSteps; i++) {
        float distance = length * i / (_gizmoSteps - 1.0f);
        Vector3 pos = transform.TransformPoint(path.GetPosition(distance));

        if (i != 0) {
          drawer.DrawLine(prev, pos);
        }

        prev = pos;
      }
    }
  }
}
