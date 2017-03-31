using UnityEngine;
using Leap.Unity;

[ExecuteInEditMode]
public class LeapGraphicComponentBase<AttachedComponent> : MonoBehaviour
  where AttachedComponent : Component {

  [SerializeField, HideInInspector]
  protected int _persistentId;

  protected virtual void Awake() {
    OnValidate();
  }

  protected virtual void OnValidate() {
    if (_persistentId == 0) {
      _persistentId = new Hash() {
        this,
        gameObject,
        name,
        transform.position,
        transform.rotation,
        transform.localScale,
        Random.Range(int.MinValue, int.MaxValue),
      };
    }

    var attatched = GetComponent<AttachedComponent>();
    if (attatched == null) {
      InternalUtility.Destroy(this);
    }

    //hideFlags = HideFlags.None;
    hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
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
