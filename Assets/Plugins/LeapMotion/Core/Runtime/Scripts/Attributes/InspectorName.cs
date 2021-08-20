/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Attributes {

  public class InspectorNameAttribute : CombinablePropertyAttribute, IFullPropertyDrawer {

    public readonly string name;

    public InspectorNameAttribute(string name) {
      this.name = name;
    }

#if UNITY_EDITOR
    public void DrawProperty(Rect rect, UnityEditor.SerializedProperty property,
                             GUIContent label) {
      label.text = name;
      UnityEditor.EditorGUI.PropertyField(rect, property, label, includeChildren: true);
    }
#endif
  }
}
