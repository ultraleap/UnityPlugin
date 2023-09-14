using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public static class SerializedPropertyExtensions 
{
    /// <summary>
    /// Returns the toString value for the serialized property value
    /// Note: returns string.Empty for serialized property types of Generic, LayerMask, Character and Gradient,
    /// as they don't have "value" properties on the serialized object
    /// </summary>
    /// <param name="serializedProperty"></param>
    /// <returns>toString of the serialized property's value</returns>
    public static string ValueToString(this SerializedProperty serializedProperty)
    {
        switch (serializedProperty.propertyType)
        {
            case SerializedPropertyType.Integer:
                return serializedProperty.intValue.ToString();
            case SerializedPropertyType.Boolean:
                return serializedProperty.boolValue.ToString();
            case SerializedPropertyType.Float:
                return serializedProperty.floatValue.ToString();
            case SerializedPropertyType.String:
                return serializedProperty.stringValue.ToString();
            case SerializedPropertyType.Color:
                return serializedProperty.colorValue.ToString();
            case SerializedPropertyType.ObjectReference:
                return serializedProperty.objectReferenceValue.ToString();
            case SerializedPropertyType.Enum:
                return serializedProperty.enumDisplayNames[serializedProperty.enumValueIndex];
            case SerializedPropertyType.Vector2:
                return serializedProperty.vector2Value.ToString();
            case SerializedPropertyType.Vector3:
                return serializedProperty.vector3Value.ToString();
            case SerializedPropertyType.Vector4:
                return serializedProperty.vector4Value.ToString();
            case SerializedPropertyType.Rect:
                return serializedProperty.rectValue.ToString();
            case SerializedPropertyType.ArraySize:
                return serializedProperty.arraySize.ToString();
            case SerializedPropertyType.AnimationCurve:
                return serializedProperty.animationCurveValue.ToString();
            case SerializedPropertyType.Bounds:
                return serializedProperty.boundsValue.ToString();
            case SerializedPropertyType.Quaternion:
                return serializedProperty.quaternionValue.ToString();
            case SerializedPropertyType.ExposedReference:
                return serializedProperty.exposedReferenceValue.ToString();
            case SerializedPropertyType.FixedBufferSize:
                return serializedProperty.fixedBufferSize.ToString();
            case SerializedPropertyType.Vector2Int:
                return serializedProperty.vector2IntValue.ToString();
            case SerializedPropertyType.Vector3Int:
                return serializedProperty.vector3IntValue.ToString();
            case SerializedPropertyType.RectInt:
                return serializedProperty.rectIntValue.ToString();
            case SerializedPropertyType.BoundsInt:
                return serializedProperty.boundsIntValue.ToString();
            case SerializedPropertyType.ManagedReference:
                return serializedProperty.managedReferenceValue.ToString();
            case SerializedPropertyType.Hash128:
                return serializedProperty.hash128Value.ToString();
            case SerializedPropertyType.Generic:
            case SerializedPropertyType.LayerMask:
            case SerializedPropertyType.Character:
            case SerializedPropertyType.Gradient:
            default:
                return string.Empty;
        }
    }
}
