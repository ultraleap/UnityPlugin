/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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
        property.vector2Value = Vector2.Max(new Vector2(minValue, minValue), property.vector2Value);
      } else if (property.propertyType == SerializedPropertyType.Vector3) {
        property.vector3Value = Vector3.Max(new Vector3(minValue, minValue, minValue), property.vector3Value);
      } else if (property.propertyType == SerializedPropertyType.Vector4) {
        property.vector4Value = Vector4.Max(new Vector4(minValue, minValue, minValue, minValue), property.vector4Value);
      }
    }

    public override IEnumerable<SerializedPropertyType> SupportedTypes {
      get {
        yield return SerializedPropertyType.Integer;
        yield return SerializedPropertyType.Float;
        yield return SerializedPropertyType.Vector2;
        yield return SerializedPropertyType.Vector3;
        yield return SerializedPropertyType.Vector4;
      }
    }
#endif
  }
}
