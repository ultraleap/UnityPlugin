/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity.Attributes {

  /// <summary>
  /// You can use this attribute to mark that an AnimationCurve can only have
  /// values that fall within specific bounds.  The user will be prevented from
  /// entering a curve that lies outside of these bounds.
  /// </summary>
  public class CurveBoundsAttribute : CombinablePropertyAttribute, IFullPropertyDrawer {
    public readonly Rect bounds;

    public CurveBoundsAttribute(Rect bounds) {
      this.bounds = bounds;
    }

    public CurveBoundsAttribute(float width, float height) {
      bounds = new Rect(0, 0, width, height);
    }

#if UNITY_EDITOR
    public void DrawProperty(Rect rect, SerializedProperty property, GUIContent label) {
      EditorGUI.CurveField(rect, property, Color.green, bounds);
    }

    public override IEnumerable<SerializedPropertyType> SupportedTypes {
      get {
        yield return SerializedPropertyType.AnimationCurve;
      }
    }
#endif
  }

  /// <summary>
  /// You can use this attribute to mark that an AnimationCurve can only have values
  /// that range from 0 to 1.  The user will be prevented from entering a curve that
  /// lies outside of these bounds.
  /// </summary>
  public class UnitCurveAttribute : CurveBoundsAttribute {
    public UnitCurveAttribute() : base(new Rect(0, 0, 1, 1)) { }
  }
}
