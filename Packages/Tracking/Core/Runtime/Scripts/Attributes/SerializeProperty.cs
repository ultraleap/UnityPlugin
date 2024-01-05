using System;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Attributes
{
    /// <summary>
    /// Usage:
    /// 
    ///class PropertySerializationTest : MonoBehaviour
    ///{
    ///    [SerializeProperty("Parameter"), SerializeField]
    ///    private _parameter;
    ///    public Parameter { get {example get;} set { example set; 
    ///}
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class SerializeProperty : PropertyAttribute
    {
        public string PropertyName { get; private set; }

        public SerializeProperty(string propertyName)
        {
            PropertyName = propertyName;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SerializeProperty))]
    public class SerializePropertyAttributeDrawer : PropertyDrawer
    {
        private PropertyInfo propertyFieldInfo = null;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            UnityEngine.Object target = property.serializedObject.targetObject;

            // Find the property field using reflection, in order to get access to its getter/setter.
            if (propertyFieldInfo == null)
                propertyFieldInfo = target.GetType().GetProperty(((SerializeProperty)attribute).PropertyName,
                                                     BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (propertyFieldInfo != null)
            {

                // Retrieve the value using the property getter:
                object value = propertyFieldInfo.GetValue(target, null);

                // Draw the property, checking for changes:
                EditorGUI.BeginChangeCheck();
                value = DrawProperty(position, property.propertyType, propertyFieldInfo.PropertyType, value, label);

                // If any changes were detected, call the property setter:
                if (EditorGUI.EndChangeCheck() && propertyFieldInfo != null)
                {

                    // Record object state for undo:
                    Undo.RecordObject(target, "Inspector");

                    // Call property setter:
                    propertyFieldInfo.SetValue(target, value, null);
                }

            }
            else
            {
                EditorGUI.LabelField(position, "Error: could not retrieve property.");
            }
        }

        private object DrawProperty(Rect position, SerializedPropertyType propertyType, Type type, object value, GUIContent label)
        {
            switch (propertyType)
            {
                case SerializedPropertyType.Integer:
                    return EditorGUI.IntField(position, label, (int)value);
                case SerializedPropertyType.Boolean:
                    return EditorGUI.Toggle(position, label, (bool)value);
                case SerializedPropertyType.Float:
                    return EditorGUI.FloatField(position, label, (float)value);
                case SerializedPropertyType.String:
                    return EditorGUI.TextField(position, label, (string)value);
                case SerializedPropertyType.Color:
                    return EditorGUI.ColorField(position, label, (Color)value);
                case SerializedPropertyType.ObjectReference:
                    return EditorGUI.ObjectField(position, label, (UnityEngine.Object)value, type, true);
                case SerializedPropertyType.ExposedReference:
                    return EditorGUI.ObjectField(position, label, (UnityEngine.Object)value, type, true);
                case SerializedPropertyType.LayerMask:
                    return EditorGUI.LayerField(position, label, (int)value);
                case SerializedPropertyType.Enum:
                    return EditorGUI.EnumPopup(position, label, (Enum)value);
                case SerializedPropertyType.Vector2:
                    return EditorGUI.Vector2Field(position, label, (Vector2)value);
                case SerializedPropertyType.Vector3:
                    return EditorGUI.Vector3Field(position, label, (Vector3)value);
                case SerializedPropertyType.Vector4:
                    return EditorGUI.Vector4Field(position, label, (Vector4)value);
                case SerializedPropertyType.Rect:
                    return EditorGUI.RectField(position, label, (Rect)value);
                case SerializedPropertyType.AnimationCurve:
                    return EditorGUI.CurveField(position, label, (AnimationCurve)value);
                case SerializedPropertyType.Bounds:
                    return EditorGUI.BoundsField(position, label, (Bounds)value);
                default:
                    throw new NotImplementedException("Unimplemented propertyType " + propertyType + ".");
            }
        }

    }
#endif
}
