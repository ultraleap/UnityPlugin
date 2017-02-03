using UnityEngine;

public class HidableMonobehaviour : MonoBehaviour {
  protected virtual void OnValidate() {
    hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
  }
}
