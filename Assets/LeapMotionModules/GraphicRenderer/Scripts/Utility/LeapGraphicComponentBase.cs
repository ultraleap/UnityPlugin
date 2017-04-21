using UnityEngine;

namespace Leap.Unity.GraphicalRenderer {

  [ExecuteInEditMode]
  public class LeapGraphicComponentBase<AttachedComponent> : MonoBehaviour
  where AttachedComponent : Component {

    protected virtual void Awake() {
      validateState();
    }

    protected virtual void OnValidate() {
      validateState();
    }

    protected virtual void OnDestroy() {
#if UNITY_EDITOR
      InternalUtility.InvokeIfUserDestroyed(OnDestroyedByUser);
#endif
    }

#if UNITY_EDITOR
    protected virtual void OnDestroyedByUser() { }
#endif

    private void validateState() {
      var attatched = GetComponent<AttachedComponent>();
      if (attatched == null) {
        InternalUtility.Destroy(this);
      }
      
      hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
      //hideFlags = HideFlags.None;
    }
  }
}
