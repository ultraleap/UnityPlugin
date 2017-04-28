/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Reflection;
using System.Collections.Generic;

namespace Leap.Unity.Attributes {

  public interface IPropertyConstrainer {
#if UNITY_EDITOR
    void ConstrainValue(SerializedProperty property);
#endif
  }

  public interface IPropertyDisabler {
#if UNITY_EDITOR
    bool ShouldDisable(SerializedProperty property);
#endif
  }

  public interface IFullPropertyDrawer {
#if UNITY_EDITOR
    void DrawProperty(Rect rect, SerializedProperty property, GUIContent label);
#endif
  }

  public interface IAdditiveDrawer {
#if UNITY_EDITOR
    float GetWidth();
    void Draw(Rect rect, SerializedProperty property);
#endif
  }

  public interface IBeforeLabelAdditiveDrawer : IAdditiveDrawer { }
  public interface IAfterLabelAdditiveDrawer : IAdditiveDrawer { }
  public interface IBeforeFieldAdditiveDrawer : IAdditiveDrawer { }
  public interface IAfterFieldAdditiveDrawer : IAdditiveDrawer { }

  public abstract class CombinablePropertyAttribute : PropertyAttribute {
    public FieldInfo fieldInfo;
    public Component component;

#if UNITY_EDITOR
    public virtual IEnumerable<SerializedPropertyType> SupportedTypes {
      get {
        yield break;
      }
    }

    public virtual void OnPropertyChanged(SerializedProperty property) { }
#endif
  }
}
