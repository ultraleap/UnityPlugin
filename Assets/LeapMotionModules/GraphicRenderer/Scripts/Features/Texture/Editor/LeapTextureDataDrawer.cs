/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEditor;

namespace Leap.Unity.GraphicalRenderer {

  [CustomPropertyDrawer(typeof(LeapTextureData))]
  public class LeapTextureDataDrawer : CustomPropertyDrawerBase {

    protected override void init(SerializedProperty property) {
      base.init(property);

      var channelFeature = LeapGraphicEditor.currentFeature as LeapTextureFeature;
      Func<string> nameFunc = () => {
        if (channelFeature == null) {
          return null;
        } else {
          return channelFeature.propertyName;
        }
      };

      drawProperty("_texture", nameFunc);
    }
  }
}
