using UnityEngine;

public class AnchorOfConstantSize : MonoBehaviour {
  void Start() { }

  public static Transform GetParentAnchorOrGui(Transform root) {
    while (true) {
      if (root == null) {
        return null;
      }

      var anchor = root.GetComponent<AnchorOfConstantSize>();
      if (anchor != null && anchor.enabled) {
        return root;
      }

      var gui = root.GetComponent<LeapGui>();
      if (gui != null) {
        return root;
      }

      root = root.parent;
    }
  }
}
