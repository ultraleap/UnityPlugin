using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
      } else {
        Debug.LogWarning("Should not use MinValue for fields that are not float or int!");
      }
    }
#endif
  }
}
