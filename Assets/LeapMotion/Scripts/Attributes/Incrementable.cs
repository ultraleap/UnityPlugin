using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Attributes {

  public class Incrementable : CombinablePropertyAttribute, IAfterFieldAdditiveDrawer {
    public const float BUTTON_WIDTH = 20;

#if UNITY_EDITOR
    public void Draw(Rect rect, SerializedProperty property) {
      if (property.propertyType != SerializedPropertyType.Integer) {
        Debug.LogWarning("Cannot use Incrementable for a field type other than Int!");
        return;
      }

      rect.width = BUTTON_WIDTH;

      if (GUI.Button(rect, "-")) {
        property.intValue--;
      }

      rect.x += rect.width;

      if (GUI.Button(rect, "+")) {
        property.intValue++;
      }
    }

    public float GetWidth() {
      return BUTTON_WIDTH * 2;
    }
#endif
  }
}
