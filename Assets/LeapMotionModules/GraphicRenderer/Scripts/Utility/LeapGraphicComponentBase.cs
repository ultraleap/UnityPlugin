using UnityEngine;

namespace Leap.Unity.GraphicalRenderer {

  [ExecuteInEditMode]
  public class LeapGraphicComponentBase<AttachedComponent> : MonoBehaviour
  where AttachedComponent : Component {

    protected virtual void Awake() {
      OnValidate();
    }

    protected virtual void OnValidate() {
      var attatched = GetComponent<AttachedComponent>();
      if (attatched == null) {
        InternalUtility.Destroy(this);
      }

      hideFlags = HideFlags.None;
      //hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
    }

    protected virtual void OnDestroy() {
#if UNITY_EDITOR
      InternalUtility.InvokeIfUserDestroyed(OnDestroyedByUser);
#endif
    }

#if UNITY_EDITOR
    protected virtual void OnDestroyedByUser() { }
#endif
  }
}
