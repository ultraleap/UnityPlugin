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
      } else if (property.propertyType == SerializedPropertyType.Vector2) {
        property.vector2Value = new Vector2(Mathf.Min(minValue, property.vector2Value.x), Mathf.Min(minValue, property.vector2Value.y));
      } else if (property.propertyType == SerializedPropertyType.Vector3) {
        property.vector3Value = new Vector3(Mathf.Min(minValue, property.vector3Value.x), Mathf.Min(minValue, property.vector3Value.y), Mathf.Max(minValue, property.vector3Value.z));
      } 
    }

    public override IEnumerable<SerializedPropertyType> SupportedTypes {
      get {
        yield return SerializedPropertyType.Integer;
        yield return SerializedPropertyType.Float;
        yield return SerializedPropertyType.Vector2;
        yield return SerializedPropertyType.Vector3;
      }
    }
#endif
  }
}
