using UnityEngine;

[ExecuteInEditMode]
public class LeapGuiComponentBase<AttatchedComponent> : MonoBehaviour
  where AttatchedComponent : Component {

  protected virtual void Awake() {
    OnValidate();
  }

  protected virtual void OnValidate() {
    var attatched = GetComponent<AttatchedComponent>();
    if (attatched == null) {
      InternalUtility.Destroy(this);
    }

    //hideFlags = HideFlags.None;
    hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
  }
}
