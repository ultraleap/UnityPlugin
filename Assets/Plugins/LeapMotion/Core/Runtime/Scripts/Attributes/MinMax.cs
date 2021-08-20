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
using System.Collections.Generic;

namespace Leap.Unity.Attributes {

  public class MinMax : CombinablePropertyAttribute, IFullPropertyDrawer {
    public const float PERCENT_NUM = 0.2f;
    public const float SPACING = 3;

    public readonly float min, max;
    public readonly bool isInt;

    public MinMax(float min, float max) {
      this.min = min;
      this.max = max;
      isInt = false;
    }

    public MinMax(int min, int max) {
      this.min = min;
      this.max = max;
      isInt = true;
    }

#if UNITY_EDITOR
    public void DrawProperty(Rect rect, SerializedProperty property, GUIContent label) {
      EditorGUI.BeginProperty(rect, label, property);

      Vector2 value = property.vector2Value;

      rect = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(FocusType.Passive), label);

      int prevIndent = EditorGUI.indentLevel;
      EditorGUI.indentLevel = 0;

      float w = rect.width * PERCENT_NUM;

      Rect leftNum = new Rect(rect.x, rect.y, w, rect.height);
      Rect slider = new Rect(rect.x + w + SPACING, rect.y, rect.width - 2 * w - SPACING * 2, rect.height);
      Rect rightNum = new Rect(rect.x + rect.width - w, rect.y, w, rect.height);

      float newMin = EditorGUI.FloatField(leftNum, value.x);
      float newMax = EditorGUI.FloatField(rightNum, value.y);

      value.x = Mathf.Clamp(newMin, min, value.y);
      value.y = Mathf.Clamp(newMax, value.x, max);

      EditorGUI.MinMaxSlider(slider, ref value.x, ref value.y, min, max);

      property.vector2Value = value;

      EditorGUI.EndProperty();

      EditorGUI.indentLevel = prevIndent;
    }

    public override IEnumerable<SerializedPropertyType> SupportedTypes {
      get {
        yield return SerializedPropertyType.Vector2;
      }
    }
#endif
  }
}
