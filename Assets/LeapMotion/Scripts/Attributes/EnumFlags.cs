using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Attributes {

  public class EnumFlags : CombinablePropertyAttribute, IFullPropertyDrawer {
    public EnumFlags() { }

#if UNITY_EDITOR
    public void DrawProperty(Rect rect, SerializedProperty property, GUIContent label) {
      property.intValue = EditorGUI.MaskField(rect, label, property.intValue, property.enumNames);
    }

    public override IEnumerable<SerializedPropertyType> SupportedTypes {
      get {
        yield return SerializedPropertyType.Enum;
      }
    }
#endif
  }
}
