/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEditor;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  [CustomPropertyDrawer(typeof(CustomChannelDataBase), useForChildren: true)]
  public class CustomChannelDataBaseDrawer : CustomPropertyDrawerBase {

    protected override void init(SerializedProperty property) {
      base.init(property);

      var channelFeature = LeapGraphicEditor.currentFeature as ICustomChannelFeature;
      Func<string> nameFunc = () => {
        if (channelFeature == null) {
          return null;
        } else {
          return channelFeature.channelName;
        }
      };

      drawProperty("_value", nameFunc);
    }
  }
}
