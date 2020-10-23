/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

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
