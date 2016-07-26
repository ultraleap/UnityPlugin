using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Attributes {

  public class UnitsAttribute : CombinablePropertyAttribute, IAfterFieldAdditiveDrawer {
    public readonly string unitsName;

    public UnitsAttribute(string unitsName) {
      this.unitsName = unitsName;
    }

#if UNITY_EDITOR
    public float GetWidth() {
      return EditorStyles.label.CalcSize(new GUIContent(unitsName)).x;
    }

    public void Draw(Rect rect, SerializedProperty property) {
      GUI.Label(rect, unitsName);
    }
#endif
  }
}
