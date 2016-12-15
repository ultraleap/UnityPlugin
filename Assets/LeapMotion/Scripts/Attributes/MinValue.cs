using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

namespace Leap.Unity.Attributes {

  public class MinValue : CombinablePropertyAttribute, IPropertyConstrainer {
    public float minValue;

    public MinValue(float minValue) {
      this.minValue = minValue;
    }

#if UNITY_EDITOR
    public void ConstrainValue(SerializedProperty property) {
      if (property.propertyType == SerializedPropertyType.Float) {
        property.floatValue = Mathf.Max(minValue, property.floatValue);
      } else if (property.propertyType == SerializedPropertyType.Integer) {
        property.intValue = Mathf.Max((int)minValue, property.intValue);
      } else if (property.propertyType == SerializedPropertyType.Vector2) {
        property.vector2Value = new Vector2(Mathf.Max(minValue, property.vector2Value.x), Mathf.Max(minValue, property.vector2Value.y));
      } else if (property.propertyType == SerializedPropertyType.Vector3) {
        property.vector3Value = new Vector3(Mathf.Max(minValue, property.vector3Value.x), Mathf.Max(minValue, property.vector3Value.y), Mathf.Max(minValue, property.vector3Value.z));
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
