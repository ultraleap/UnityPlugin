using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Attributes {

  public class MaxValue : CombinablePropertyAttribute, IPropertyConstrainer {
    public float maxValue;

    public MaxValue(float maxValue) {
      this.maxValue = maxValue;
    }

#if UNITY_EDITOR
    public void ConstrainValue(SerializedProperty property) {
      if (property.propertyType == SerializedPropertyType.Float) {
        property.floatValue = Mathf.Min(maxValue, property.floatValue);
      } else if (property.propertyType == SerializedPropertyType.Integer) {
        property.intValue = Mathf.Min((int)maxValue, property.intValue);
      }
    }

    public override IEnumerable<SerializedPropertyType> SupportedTypes {
      get {
        yield return SerializedPropertyType.Integer;
        yield return SerializedPropertyType.Float;
      }
    }
#endif
  }
}
