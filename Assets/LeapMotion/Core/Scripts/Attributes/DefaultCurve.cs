using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity.Attributes {

  public enum CurveType {
    Zero,
    Constant,
    LinearUp,
    LinearDown,
    SigmoidUp,
    SigmoidDown
  }

  public class DefaultCurve : CombinablePropertyAttribute, IPropertyConstrainer {
    public readonly CurveType curveType;
    public readonly WrapMode wrapMode;
    public readonly float width;
    public readonly float height;

    public DefaultCurve(CurveType curveType, float width = 1, float height = 1, WrapMode wrapMode = WrapMode.ClampForever) {
      this.curveType = curveType;
      this.wrapMode = wrapMode;
      this.width = width;
      this.height = height;
    }

#if UNITY_EDITOR
    public void ConstrainValue(SerializedProperty property) {
      var curveValue = property.animationCurveValue;
      if (curveValue.length == 0) {
        switch (curveType) {
          case CurveType.Zero:
            curveValue.AddKey(0, 0);
            curveValue.AddKey(width, 0);
            break;
          case CurveType.Constant:
            curveValue.AddKey(0, height);
            curveValue.AddKey(width, height);
            break;
          case CurveType.LinearUp:
            curveValue.AddKey(new Keyframe(0, 0, height / width, height / width));
            curveValue.AddKey(new Keyframe(width, height, height / width, height / width));
            break;
          case CurveType.LinearDown:
            curveValue.AddKey(new Keyframe(0, height, -height / width, -height / width));
            curveValue.AddKey(new Keyframe(width, 0, -height / width, -height / width));
            break;
          case CurveType.SigmoidUp:
            curveValue.AddKey(new Keyframe(0, 0, 0, 0));
            curveValue.AddKey(new Keyframe(width, height, 0, 0));
            break;
          case CurveType.SigmoidDown:
            curveValue.AddKey(new Keyframe(0, height, 0, 0));
            curveValue.AddKey(new Keyframe(width, 0, 0, 0));
            break;
          default:
            Debug.LogError("Unexpected curve type: " + curveType);
            break;
        }

        curveValue.preWrapMode = wrapMode;
        curveValue.postWrapMode = wrapMode;
        property.animationCurveValue = curveValue;
      }
    }

    public override IEnumerable<SerializedPropertyType> SupportedTypes {
      get {
        yield return SerializedPropertyType.AnimationCurve;
      }
    }
#endif
  }
}
