/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.GraphicalRenderer {

  [CustomPropertyDrawer(typeof(LeapTextRenderer))]
  public class LeapTextRendererDrawer : CustomPropertyDrawerBase {
    private static float HELP_BOX_HEIGHT = EditorGUIUtility.singleLineHeight * 2;

    protected override void init(SerializedProperty property) {
      base.init(property);

      drawCustom(rect => { }, EditorGUIUtility.singleLineHeight * 0.5f);

      drawCustom(rect => EditorGUI.LabelField(rect, "Text Settings", EditorStyles.boldLabel),
                 EditorGUIUtility.singleLineHeight);

      var fontProp = property.FindPropertyRelative("_font");
      drawCustom(rect => {
        Font font = fontProp.objectReferenceValue as Font;
        if (font != null && !font.dynamic) {
          rect.height = HELP_BOX_HEIGHT;
          EditorGUI.HelpBox(rect, "Only dynamic fonts are currently supported.", MessageType.Error);
          rect.y += HELP_BOX_HEIGHT;
        }
        rect.height = EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(rect, fontProp);
      }, () => {
        Font font = fontProp.objectReferenceValue as Font;
        if (font != null && !font.dynamic) {
          return HELP_BOX_HEIGHT + EditorGUIUtility.singleLineHeight;
        } else {
          return EditorGUIUtility.singleLineHeight;
        }
      });

      drawProperty("_dynamicPixelsPerUnit");
      drawProperty("_useColor");
      drawPropertyConditionally("_globalTint", "_useColor");
      drawProperty("_shader");
      drawProperty("_scale");
    }

  }
}

