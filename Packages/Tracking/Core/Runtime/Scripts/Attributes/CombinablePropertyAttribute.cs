/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Reflection;
using System.Collections.Generic;

namespace Leap.Unity.Attributes
{

    using UnityObject = UnityEngine.Object;

    public interface IPropertyConstrainer
    {
#if UNITY_EDITOR
        void ConstrainValue(SerializedProperty property);
#endif
    }

    public interface IPropertyDisabler
    {
#if UNITY_EDITOR
        bool ShouldDisable(SerializedProperty property);
#endif
    }

    public interface IFullPropertyDrawer
    {
#if UNITY_EDITOR
        void DrawProperty(Rect rect, SerializedProperty property, GUIContent label);
#endif
    }

    public interface IAdditiveDrawer
    {
#if UNITY_EDITOR
        float GetWidth();
        void Draw(Rect rect, SerializedProperty property);
#endif
    }

    public interface ITopPanelDrawer
    {
#if UNITY_EDITOR
        float GetHeight();
        void Draw(Rect panelRect, SerializedProperty property);
#endif
    }

    public interface ISupportDragAndDrop
    {
#if UNITY_EDITOR
        Rect GetDropArea(Rect r, SerializedProperty property);
        bool IsDropValid(UnityObject[] draggedObjects, SerializedProperty property);
        void ProcessDroppedObjects(UnityObject[] droppedObjects, SerializedProperty property);
#endif
    }

    public interface IBeforeLabelAdditiveDrawer : IAdditiveDrawer { }
    public interface IAfterLabelAdditiveDrawer : IAdditiveDrawer { }
    public interface IBeforeFieldAdditiveDrawer : IAdditiveDrawer { }
    public interface IAfterFieldAdditiveDrawer : IAdditiveDrawer { }

    public abstract class CombinablePropertyAttribute : PropertyAttribute
    {
        private bool _isInitialized = false;

        private FieldInfo _fieldInfo;
        public FieldInfo fieldInfo
        {
            get
            {
                if (!_isInitialized)
                {
                    Debug.LogError("CombinablePropertyAttribute needed fieldInfo but was not "
                                 + "initialized. Did you call Init()?");
                }
                return _fieldInfo;
            }
            protected set
            {
                _fieldInfo = value;
            }
        }

        private UnityObject[] _targets;
        public UnityObject[] targets
        {
            get
            {
                if (!_isInitialized)
                {
                    Debug.LogError("CombinablePropertyAttribute needed fieldInfo but was not "
                                 + "initialized. Did you call Init()?");
                }
                return _targets;
            }
            protected set
            {
                _targets = value;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Initializes the FieldInfo and target information for this
        /// CombinablePropertyAttribute using a SerializedProperty reference.
        /// 
        /// This requires reflection, so it involves a bit more work than if you already have
        /// FieldInfo and target information.
        /// </summary>
        public void Init(SerializedProperty property)
        {
            var propertyName = property.name;
            var serializedObject = property.serializedObject;
            var targetObjectType = serializedObject.targetObject.GetType();
            fieldInfo = targetObjectType.GetField(propertyName,
              BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance
              | BindingFlags.FlattenHierarchy);

            targets = property.serializedObject.targetObjects;

            _isInitialized = true;
        }

        /// <summary>
        /// Initializes data for the CombinablePropertyAttribute using the provided field
        /// and target information.
        /// </summary>
        public void Init(FieldInfo fieldInfo, UnityObject[] targets)
        {
            this.fieldInfo = fieldInfo;
            this.targets = targets;

            _isInitialized = true;
        }

        public virtual IEnumerable<SerializedPropertyType> SupportedTypes
        {
            get
            {
                yield break;
            }
        }

        public virtual void OnPropertyChanged(SerializedProperty property) { }
#endif
    }
}