using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Attributes {

  public class InspectorNameAttribute : CombinablePropertyAttribute, IFullPropertyDrawer {

    public readonly string name;

    public InspectorNameAttribute(string name) {
      this.name = name;
    }

#if UNITY_EDITOR
    public void DrawProperty(Rect rect, SerializedProperty property, GUIContent label) {
      label.text = name;
      EditorGUI.PropertyField(rect, property, label, includeChildren: true);
    }
#endif
  }
}
