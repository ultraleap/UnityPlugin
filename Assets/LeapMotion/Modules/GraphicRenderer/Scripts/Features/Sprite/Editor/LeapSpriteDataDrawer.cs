/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.GraphicalRenderer {

  [CustomPropertyDrawer(typeof(LeapSpriteData))]
  public class LeapSpriteDataDrawer : CustomPropertyDrawerBase {

    protected override void init(SerializedProperty property) {
      base.init(property);

      var channelFeature = LeapGraphicEditor.currentFeature as LeapSpriteFeature;
      Func<string> nameFunc = () => {
        if (channelFeature == null) {
          return null;
        } else {
          return channelFeature.propertyName;
        }
      };

      var spriteProp = property.FindPropertyRelative("_sprite");

      drawCustom(rect => {
        if (rect.height != 0) {
          var indentedRect = EditorGUI.IndentedRect(rect);
          EditorGUI.HelpBox(indentedRect, "Sprite is not packed!", MessageType.Error);
        }
      }, () => {
        Sprite sprite = spriteProp.objectReferenceValue as Sprite;
        if (sprite != null && !sprite.packed) {
          return EditorGUIUtility.singleLineHeight * 2;
        } else {
          return 0;
        }
      });

      drawProperty("_sprite", nameFunc);
    }
  }
}
