/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity
{

    public class CustomEditorBase<T> : CustomEditorBase where T : UnityEngine.Object
    {
        protected new T target;
        protected new T[] targets;

        protected override void OnEnable()
        {
            base.OnEnable();

            target = base.target as T;
            targets = base.targets.Where(t => t != null).
                                   OfType<T>().
                                   ToArray();
        }
    }

    public class CustomEditorBase : Editor
    {
        protected Dictionary<string, Action<SerializedProperty>> _specifiedDrawers;
        protected Dictionary<string, List<Action<SerializedProperty>>> _specifiedDecorators;
        protected Dictionary<string, List<Action<SerializedProperty>>> _specifiedPostDecorators;
        protected Dictionary<string, List<Func<bool>>> _conditionalProperties;
        protected Dictionary<string, List<string>> _foldoutProperties;
        protected Dictionary<string, bool> _foldoutStates;
        protected List<string> _deferredProperties;
        protected bool _showScriptField = true;
        protected bool _drawFoldoutInLine = false;
        protected Dictionary<string, bool> _foldoutDrawn;

        private bool _canCallSpecifyFunctions = false;
        private GUIStyle _boldFoldoutStyle;

        protected List<SerializedProperty> _modifiedProperties = new List<SerializedProperty>();

        protected void dontShowScriptField()
        {
            _showScriptField = false;
        }

        protected void drawFoldoutInLine()
        {
            _drawFoldoutInLine = true;
        }

        /// <summary>
        /// Specify a callback to be used to draw a specific named property.  Should be called in OnEnable.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyDrawer"></param>
        protected void specifyCustomDrawer(string propertyName, Action<SerializedProperty> propertyDrawer)
        {
            throwIfNotInOnEnable("specifyCustomDrawer");

            if (!validateProperty(propertyName))
            {
                return;
            }

            _specifiedDrawers[propertyName] = propertyDrawer;
        }

        /// <summary>
        /// Specify a callback to be used to draw a decorator for a specific named property.  Should be called in OnEnable.
        /// </summary>
        protected void specifyCustomDecorator(string propertyName, Action<SerializedProperty> decoratorDrawer)
        {
            throwIfNotInOnEnable("specifyCustomDecorator");

            if (!validateProperty(propertyName))
            {
                return;
            }

            List<Action<SerializedProperty>> list;
            if (!_specifiedDecorators.TryGetValue(propertyName, out list))
            {
                list = new List<Action<SerializedProperty>>();
                _specifiedDecorators[propertyName] = list;
            }

            list.Add(decoratorDrawer);
        }

        /// <summary>
        /// Specify a callback to be used to draw a decorator AFTER a specific named property.
        /// 
        /// Should be called in OnEnable.
        /// </summary>
        protected void specifyCustomPostDecorator(string propertyName, Action<SerializedProperty> decoratorDrawer)
        {
            throwIfNotInOnEnable("specifyCustomPostDecorator");

            if (!validateProperty(propertyName))
            {
                return;
            }

            List<Action<SerializedProperty>> list;
            if (!_specifiedPostDecorators.TryGetValue(propertyName, out list))
            {
                list = new List<Action<SerializedProperty>>();
                _specifiedPostDecorators[propertyName] = list;
            }

            list.Add(decoratorDrawer);
        }

        /// <summary>
        /// Specify a list of properties that should only be displayed if the conditional property has a value of true.
        /// Should be called in OnEnable.
        /// </summary>
        /// <param name="conditionalName"></param>
        /// <param name="dependantProperties"></param>
        protected void specifyConditionalDrawing(string conditionalName, params string[] dependantProperties)
        {
            throwIfNotInOnEnable("specifyConditionalDrawing");

            if (!validateProperty(conditionalName))
            {
                return;
            }

            SerializedProperty conditionalProp = serializedObject.FindProperty(conditionalName);
            specifyConditionalDrawing(() =>
            {
                if (conditionalProp.hasMultipleDifferentValues)
                {
                    return false;
                }
                else
                {
                    return conditionalProp.boolValue;
                }
            }, dependantProperties);
        }

        protected void specifyConditionalDrawing(string enumName, int enumValue, params string[] dependantProperties)
        {
            throwIfNotInOnEnable("specifyConditionalDrawing");

            if (!validateProperty(enumName))
            {
                return;
            }

            SerializedProperty enumProp = serializedObject.FindProperty(enumName);
            specifyConditionalDrawing(() =>
            {
                if (enumProp.hasMultipleDifferentValues)
                {
                    return false;
                }
                else
                {
                    return enumProp.intValue == enumValue;
                }
            }, dependantProperties);
        }

        protected void specifyConditionalDrawing(string enumName, int[] enumValues, params string[] dependantProperties)
        {
            throwIfNotInOnEnable("specifyConditionalDrawing");

            if (!validateProperty(enumName))
            {
                return;
            }

            SerializedProperty enumProp = serializedObject.FindProperty(enumName);
            specifyConditionalDrawing(() =>
            {
                if (enumProp.hasMultipleDifferentValues)
                {
                    return false;
                }
                else
                {
                    return enumValues.Contains(enumProp.intValue);
                }
            }, dependantProperties);
        }

        protected void hideField(string propertyName)
        {
            throwIfNotInOnEnable("hideField");

            specifyConditionalDrawing(() => false, propertyName);
        }

        protected void specifyConditionalDrawing(Func<bool> conditional, params string[] dependantProperties)
        {
            throwIfNotInOnEnable("specifyConditionalDrawing");

            for (int i = 0; i < dependantProperties.Length; i++)
            {
                string dependant = dependantProperties[i];

                if (!validateProperty(dependant))
                {
                    continue;
                }

                List<Func<bool>> list;
                if (!_conditionalProperties.TryGetValue(dependant, out list))
                {
                    list = new List<Func<bool>>();
                    _conditionalProperties[dependant] = list;
                }
                list.Add(conditional);
            }
        }

        /// <summary>
        /// Defer rendering of a property until the end of the inspector.  Deferred properties
        /// are drawn in the REVERSE order they are deferred!  NOT by the order they appear in the 
        /// serialized object!
        /// </summary>
        protected void deferProperty(string propertyName)
        {
            throwIfNotInOnEnable("deferProperty");

            if (!validateProperty(propertyName))
            {
                return;
            }

            _deferredProperties.Insert(0, propertyName);
        }

        /// <summary>
        /// Condition the drawing of a property based on the status of a foldout drop-down.
        /// </summary>
        protected void addPropertyToFoldout(string propertyName, string foldoutName, bool foldoutStartOpen = false)
        {
            throwIfNotInOnEnable("addPropertyToFoldout");

            if (!validateProperty(propertyName)) { return; }

            List<string> list;
            if (!_foldoutProperties.TryGetValue(foldoutName, out list))
            {
                list = new List<string>();
                _foldoutProperties[foldoutName] = list;
            }
            _foldoutProperties[foldoutName].Add(propertyName);
            _foldoutStates[foldoutName] = foldoutStartOpen;
        }

        /// <summary>
        /// Check whether a property is inside of a foldout drop-down.
        /// </summary>
        protected bool isInFoldout(string propertyName)
        {
            bool isInFoldout = false;
            foreach (var foldout in _foldoutProperties)
            {
                foreach (string property in foldout.Value)
                {
                    if (property.Equals(propertyName)) { isInFoldout = true; break; }
                }
                if (isInFoldout) { break; }
            }
            return isInFoldout;
        }

        protected void drawScriptField(bool disable = true)
        {
            var scriptProp = serializedObject.FindProperty("m_Script");
            EditorGUI.BeginDisabledGroup(disable);
            EditorGUILayout.PropertyField(scriptProp);
            EditorGUI.EndDisabledGroup();
        }

        protected virtual void OnEnable()
        {
            try
            {
                if (serializedObject == null) { }
            }
            catch (NullReferenceException)
            {
                DestroyImmediate(this);
                throw new Exception("Cleaning up an editor of type " + GetType() + ".  Make sure to always destroy your editors when you are done with them!");
            }

            _specifiedDrawers = new Dictionary<string, Action<SerializedProperty>>();
            _specifiedDecorators = new Dictionary<string, List<Action<SerializedProperty>>>();
            _specifiedPostDecorators = new Dictionary<string, List<Action<SerializedProperty>>>();
            _conditionalProperties = new Dictionary<string, List<Func<bool>>>();
            _foldoutProperties = new Dictionary<string, List<string>>();
            _foldoutStates = new Dictionary<string, bool>();
            _foldoutDrawn = new Dictionary<string, bool>();
            _deferredProperties = new List<string>();
            _canCallSpecifyFunctions = true;
        }

        protected bool validateProperty(string propertyName)
        {
            if (serializedObject.FindProperty(propertyName) == null)
            {
                Debug.LogWarning("Property " + propertyName + " does not exist, was it removed or renamed?");
                return false;
            }
            return true;
        }

        /* 
         * This method draws all visible properties, mirroring the default behavior of OnInspectorGUI. 
         * Individual properties can be specified to have custom drawers.
         */
        public override void OnInspectorGUI()
        {
            // OnInspectorGUI is the first time "EditorStyles" can be accessed
            if (_boldFoldoutStyle == null)
            {
                _boldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
                _boldFoldoutStyle.fontStyle = FontStyle.Bold;
            }

            _canCallSpecifyFunctions = false;

            _foldoutDrawn.Clear();
            _modifiedProperties.Clear();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool isFirst = true;

            while (iterator.NextVisible(isFirst))
            {
                if (isFirst && !_showScriptField)
                {
                    isFirst = false;
                    continue;
                }

                if (_deferredProperties.Contains(iterator.name))
                {
                    continue;
                }

                if (isInFoldout(iterator.name))
                {
                    if (_drawFoldoutInLine)
                    {
                        foreach (var foldout in _foldoutProperties)
                        {
                            if (_foldoutDrawn.ContainsKey(foldout.Key) && _foldoutDrawn[foldout.Key]) { continue; }

                            foreach (string property in foldout.Value)
                            {
                                if (property.Equals(iterator.name))
                                {
                                    drawFoldout(foldout);
                                    if (_foldoutStates[foldout.Key])
                                    {
                                        EditorGUILayout.Space();
                                    }

                                    _foldoutDrawn[foldout.Key] = true;
                                    break;
                                }
                            }
                        }
                    }
                    continue;
                }

                using (new EditorGUI.DisabledGroupScope(isFirst))
                {
                    drawProperty(iterator);
                }

                isFirst = false;
            }

            foreach (var deferredProperty in _deferredProperties)
            {
                if (!isInFoldout(deferredProperty))
                {
                    drawProperty(serializedObject.FindProperty(deferredProperty));
                }
            }

            if (!_drawFoldoutInLine)
            {
                foreach (var foldout in _foldoutProperties)
                {
                    drawFoldout(foldout);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void drawFoldout(KeyValuePair<string, List<string>> foldout)
        {
            _foldoutStates[foldout.Key] =
                      EditorGUILayout.Foldout(_foldoutStates[foldout.Key], foldout.Key, _boldFoldoutStyle);
            if (_foldoutStates[foldout.Key])
            {
                // Draw normal priority properties first
                foreach (var property in foldout.Value)
                {
                    if (!_deferredProperties.Contains(property))
                    {
                        drawProperty(serializedObject.FindProperty(property));
                    }
                }
                // Draw deferred properties second
                foreach (var property in foldout.Value)
                {
                    if (_deferredProperties.Contains(property))
                    {
                        drawProperty(serializedObject.FindProperty(property));
                    }
                }
            }
        }

        private void drawProperty(SerializedProperty property)
        {
            List<Func<bool>> conditionalList;
            if (_conditionalProperties.TryGetValue(property.name, out conditionalList))
            {
                bool allTrue = true;
                for (int i = 0; i < conditionalList.Count; i++)
                {
                    allTrue &= conditionalList[i]();
                }
                if (!allTrue)
                {
                    return;
                }
            }

            Action<SerializedProperty> customDrawer;

            List<Action<SerializedProperty>> decoratorList;
            if (_specifiedDecorators.TryGetValue(property.name, out decoratorList))
            {
                for (int i = 0; i < decoratorList.Count; i++)
                {
                    decoratorList[i](property);
                }
            }

            EditorGUI.BeginChangeCheck();

            if (_specifiedDrawers.TryGetValue(property.name, out customDrawer))
            {
                customDrawer(property);
            }
            else
            {
                EditorGUILayout.PropertyField(property, true);
            }

            if (EditorGUI.EndChangeCheck())
            {
                _modifiedProperties.Add(property.Copy());
            }


            List<Action<SerializedProperty>> postDecoratorList;
            if (_specifiedPostDecorators.TryGetValue(property.name, out postDecoratorList))
            {
                for (int i = 0; i < postDecoratorList.Count; i++)
                {
                    postDecoratorList[i](property);
                }
            }
        }

        private void throwIfNotInOnEnable(string methodName)
        {
            if (!_canCallSpecifyFunctions)
            {
                throw new InvalidOperationException("Cannot call " + methodName + " from within any other function but OnEnable.  Make sure you also call base.OnEnable as well!");
            }
        }
    }
}