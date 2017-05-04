/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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
        property.vector2Value = Vector2.Min(new Vector2(maxValue, maxValue), property.vector2Value);
      } else if (property.propertyType == SerializedPropertyType.Vector3) {
        property.vector3Value = Vector3.Min(new Vector3(maxValue, maxValue, maxValue), property.vector3Value);
      } else if (property.propertyType == SerializedPropertyType.Vector4) {
        property.vector4Value = Vector4.Min(new Vector4(maxValue, maxValue, maxValue, maxValue), property.vector4Value);
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
