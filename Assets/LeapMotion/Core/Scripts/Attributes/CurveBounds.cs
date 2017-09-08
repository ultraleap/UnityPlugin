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
  public class CurveBounds : CombinablePropertyAttribute, IFullPropertyDrawer {
    public readonly Rect bounds;
    public readonly Color color;

    public CurveBounds(Rect bounds) {
      this.bounds = bounds;
      color = Color.green;
    }

    public CurveBounds(Rect bounds, Color color) {
      this.bounds = bounds;
      this.color = color;
    }

    public CurveBounds(float width, float height) {
      bounds = new Rect(0, 0, width, height);
      color = Color.green;
    }

    public CurveBounds(float width, float height, Color color) {
      bounds = new Rect(0, 0, width, height);
      this.color = color;
    }

#if UNITY_EDITOR
    public void DrawProperty(Rect rect, SerializedProperty property, GUIContent label) {
      EditorGUI.CurveField(rect, property, color, bounds);
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
  public class UnitCurve : CurveBounds {
    public UnitCurve() : base(new Rect(0, 0, 1, 1), Color.green) { }
    public UnitCurve(Color color) : base(new Rect(0, 0, 1, 1), color) { }
  }
}
