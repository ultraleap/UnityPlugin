/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Attributes
{

    public class MaxValue : CombinablePropertyAttribute, IPropertyConstrainer
    {
        public float maxValue;

        public MaxValue(float maxValue)
        {
            this.maxValue = maxValue;
        }

#if UNITY_EDITOR
        public void ConstrainValue(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.Float)
            {
                property.floatValue = Mathf.Min(maxValue, property.floatValue);
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = Mathf.Min((int)maxValue, property.intValue);
            }
            else if (property.propertyType == SerializedPropertyType.Vector2)
            {
                property.vector2Value = Vector2.Min(new Vector2(maxValue, maxValue), property.vector2Value);
            }
            else if (property.propertyType == SerializedPropertyType.Vector3)
            {
                property.vector3Value = Vector3.Min(new Vector3(maxValue, maxValue, maxValue), property.vector3Value);
            }
            else if (property.propertyType == SerializedPropertyType.Vector4)
            {
                property.vector4Value = Vector4.Min(new Vector4(maxValue, maxValue, maxValue, maxValue), property.vector4Value);
            }
        }

        public override IEnumerable<SerializedPropertyType> SupportedTypes
        {
            get
            {
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