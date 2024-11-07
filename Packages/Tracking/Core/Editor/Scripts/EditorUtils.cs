/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Leap
{
    using UnityObject = UnityEngine.Object;

    public static class EditorUtils
    {
        /// <summary>
        /// Scans the currently-open scene for references of a and replaces them with b.
        /// These swaps are performed with Undo.RecordObject before an object's references
        /// are changed, so they can be undone.
        /// </summary>
        public static void ReplaceSceneReferences<T>(T a, T b) where T : UnityObject
        {
            var aId = a.GetInstanceID();
            var refType = typeof(T);

            var curScene = SceneManager.GetActiveScene();
            var rootObjs = curScene.GetRootGameObjects();
            foreach (var rootObj in rootObjs)
            {
                var transforms = rootObj.GetComponentsInChildren<Transform>();
                foreach (var transform in transforms)
                {
                    var components = transform.GetComponents<Component>();

                    var objectChanges = new List<Action>();
                    foreach (var component in components)
                    {
                        var compType = typeof(Component);
                        var fieldInfos = compType.GetFields(BindingFlags.Instance
                          | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic
                          | BindingFlags.Public);
                        foreach (var fieldInfo in fieldInfos.
                            Where(fi => fi.FieldType.IsAssignableFrom(refType)))
                        {
                            var refValue = fieldInfo.GetValue(component) as T;
                            if (refValue.GetInstanceID() == aId)
                            {
                                objectChanges.Add(() =>
                                {
                                    fieldInfo.SetValue(fieldInfo, b);
                                });
                            }
                        }
                    }
                    if (objectChanges.Count > 0)
                    {
                        Undo.RecordObject(transform.gameObject,
                          "Swap " + transform.name + " references from " + a.name + " to " + b.name);
                        foreach (var change in objectChanges)
                        {
                            change();
                        }
                    }
                }
            }
        }

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

        public static void DrawScriptField(MonoBehaviour target)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(target), target.GetType(), false);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();
        }
    }
}