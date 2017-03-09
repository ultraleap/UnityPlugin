using UnityEngine;

[ExecuteInEditMode]
public class LeapGuiComponentBase<AttachedComponent> : MonoBehaviour
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
}
