using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity.Attributes {

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

  public class UnitCurve : CombinablePropertyAttribute, IFullPropertyDrawer {
    public readonly Color color;

    public UnitCurve() {
      color = Color.green;
    }

    public UnitCurve(Color color) {
      this.color = color;
    }

#if UNITY_EDITOR
    public void DrawProperty(Rect rect, SerializedProperty property, GUIContent label) {
      EditorGUI.CurveField(rect, property, color, new Rect(0, 0, 1, 1));
    }

    public override IEnumerable<SerializedPropertyType> SupportedTypes {
      get {
        yield return SerializedPropertyType.AnimationCurve;
      }
    }
#endif
  }
}
