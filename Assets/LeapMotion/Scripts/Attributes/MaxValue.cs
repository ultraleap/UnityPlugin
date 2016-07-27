using UnityEngine;
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
      } else {
        Debug.LogWarning("Should not use MaxValue for fields that are not float or int!");
      }
    }
#endif
  }
}
