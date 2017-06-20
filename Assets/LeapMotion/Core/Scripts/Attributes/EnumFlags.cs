/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Attributes {

  public class EnumFlags : CombinablePropertyAttribute, IFullPropertyDrawer {
    public EnumFlags() { }

#if UNITY_EDITOR
    public void DrawProperty(Rect rect, SerializedProperty property, GUIContent label) {
      property.intValue = EditorGUI.MaskField(rect, label, property.intValue, property.enumNames);
    }

    public override IEnumerable<SerializedPropertyType> SupportedTypes {
      get {
        yield return SerializedPropertyType.Enum;
      }
    }
#endif
  }
}
