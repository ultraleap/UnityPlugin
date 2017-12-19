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
  using Query;

  public class EnumFlags : CombinablePropertyAttribute, IFullPropertyDrawer {

    public EnumFlags() { }

#if UNITY_EDITOR
    private string[] _enumNames;
    private int[] _enumValues;

    public void DrawProperty(Rect rect, SerializedProperty property, GUIContent label) {
      if (_enumNames == null) {
        string[] names = (string[])Enum.GetNames(fieldInfo.FieldType);
        int[] values = (int[])Enum.GetValues(fieldInfo.FieldType);

        int count = values.Query().Count(v => v != 0);
        _enumNames = new string[count];
        _enumValues = new int[count];

        int index = 0;
        for (int i = 0; i < names.Length; i++) {
          if (values[i] == 0) continue;

          _enumNames[index] = names[i];
          _enumValues[index] = values[i];
          index++;
        }
      }

      int convertedMask = 0;
      for (int i = 0; i < _enumValues.Length; i++) {
        if ((property.intValue & _enumValues[i]) != 0) {
          convertedMask |= (1 << i);
        }
      }

      int resultMask = EditorGUI.MaskField(rect, label, convertedMask, _enumNames);

      int propertyMask = 0;
      {
        int index = 0;
        while (resultMask != 0 && index < _enumValues.Length) {
          if ((resultMask & 1) != 0) {
            propertyMask |= _enumValues[index];
          }

          index++;
          resultMask = resultMask >> 1;
        }
      }

      property.intValue = propertyMask;
    }

    public override IEnumerable<SerializedPropertyType> SupportedTypes {
      get {
        yield return SerializedPropertyType.Enum;
      }
    }
#endif
  }
}
