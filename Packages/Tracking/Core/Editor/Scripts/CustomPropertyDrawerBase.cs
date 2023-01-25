/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity
{

    public class CustomPropertyDrawerBase : PropertyDrawer
    {
        public const float INDENT_AMOUNT = 12;

        private List<IDrawable> _drawables;
        private SerializedProperty _property;

        private string _onGuiSampleName;
        private string _getHeightSampleName;

        public CustomPropertyDrawerBase()
        {
            _onGuiSampleName = "OnGUI for " + GetType().Name;
            _getHeightSampleName = "GetPropertyHeight for " + GetType().Name;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            init(property);

            foreach (var drawable in _drawables)
            {
                drawable.Draw(ref position);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            init(property);

            float height = 0;
            foreach (var drawable in _drawables)
            {
                if (drawable is PropertyContainer)
                {
                    height += ((PropertyContainer)drawable).getHeight();
                }
            }

            return height;
        }

        protected virtual void init(SerializedProperty property)
        {
            if (_property == property)
            {
                return;
            }

            _drawables = new List<IDrawable>();
            _property = property;
        }

        protected void drawPropertyConditionally(string propertyName, string conditionalName, bool includeChildren = true)
        {
            SerializedProperty property, condition;
            if (!tryGetProperty(propertyName, out property) || !tryGetProperty(conditionalName, out condition))
            {
                return;
            }

            _drawables.Add(new PropertyContainer()
            {
                draw = rect =>
                {
                    if (condition.boolValue)
                    {
                        EditorGUI.PropertyField(rect, property, includeChildren);
                    }
                },
                getHeight = () =>
                {
                    return condition.boolValue ? EditorGUI.GetPropertyHeight(property, GUIContent.none, includeChildren) : 0;
                }
            });
        }

        protected void drawPropertyConditionally(string propertyName, Func<bool> condition, bool includeChildren = true)
        {
            SerializedProperty property;
            if (!tryGetProperty(propertyName, out property))
            {
                return;
            }

            _drawables.Add(new PropertyContainer()
            {
                draw = rect =>
                {
                    if (condition())
                    {
                        EditorGUI.PropertyField(rect, property, includeChildren);
                    }
                },
                getHeight = () =>
                {
                    return condition() ? EditorGUI.GetPropertyHeight(property, GUIContent.none, includeChildren) : 0;
                }
            });
        }

        protected void drawProperty(string name, bool includeChildren = true, bool disable = false)
        {
            SerializedProperty property;
            if (!tryGetProperty(name, out property))
            {
                return;
            }

            GUIContent content = new GUIContent(property.displayName, property.tooltip);
            _drawables.Add(new PropertyContainer()
            {
                draw = rect =>
                {
                    EditorGUI.BeginDisabledGroup(disable);
                    EditorGUI.PropertyField(rect, property, content, includeChildren);
                    EditorGUI.EndDisabledGroup();
                },
                getHeight = () => EditorGUI.GetPropertyHeight(property, GUIContent.none, includeChildren)
            });
        }

        protected void drawProperty(string name, Func<string> nameFunc, bool includeChildren = true)
        {
            SerializedProperty property;
            if (!tryGetProperty(name, out property))
            {
                return;
            }

            GUIContent content = new GUIContent(nameFunc(), property.tooltip);

            _drawables.Add(new PropertyContainer()
            {
                draw = rect =>
                {
                    content.text = nameFunc() ?? property.displayName;
                    EditorGUI.PropertyField(rect, property, content, includeChildren);
                },
                getHeight = () => EditorGUI.GetPropertyHeight(property, content, includeChildren)
            });
        }

        protected void drawCustom(Action<Rect> drawFunc, float height)
        {
            _drawables.Add(new PropertyContainer()
            {
                draw = drawFunc,
                getHeight = () => height
            });
        }

        protected void drawCustom(Action<Rect> drawFunc, Func<float> heightFunc)
        {
            _drawables.Add(new PropertyContainer()
            {
                draw = drawFunc,
                getHeight = heightFunc
            });
        }

        protected void increaseIndent()
        {
            _drawables.Add(new IndentDrawable()
            {
                indent = INDENT_AMOUNT
            });
        }

        protected void decreaseIndent()
        {
            _drawables.Add(new IndentDrawable()
            {
                indent = -INDENT_AMOUNT
            });
        }

        protected bool tryGetProperty(string name, out SerializedProperty property)
        {
            property = _property.FindPropertyRelative(name);

            if (property == null)
            {
                Debug.LogWarning("Could not find property " + name + ", was it renamed or removed?");
                return false;
            }
            else
            {
                return true;
            }
        }

        protected bool validateProperty(string name)
        {
            if (_property.FindPropertyRelative(name) == null)
            {
                Debug.LogWarning("Could not find property " + name + ", was it renamed or removed?");
                return false;
            }

            return true;
        }

        private interface IDrawable
        {
            void Draw(ref Rect rect);
        }

        private struct PropertyContainer : IDrawable
        {
            public Action<Rect> draw;
            public Func<float> getHeight;

            public void Draw(ref Rect rect)
            {
                rect.height = getHeight();
                draw(rect);
                rect.y += rect.height;
            }
        }

        private struct IndentDrawable : IDrawable
        {
            public float indent;

            public void Draw(ref Rect rect)
            {
                rect.x += indent;
                rect.width -= indent;
            }
        }
    }
}