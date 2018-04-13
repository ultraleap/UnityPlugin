/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEditor;

namespace Leap.Unity.GraphicalRenderer {

  [CustomPropertyDrawer(typeof(CustomFloatChannelFeature))]
  [CustomPropertyDrawer(typeof(CustomVectorChannelFeature))]
  [CustomPropertyDrawer(typeof(CustomColorChannelFeature))]
  [CustomPropertyDrawer(typeof(CustomMatrixChannelFeature))]
  public class CustomChannelFeatureDrawer : CustomPropertyDrawerBase {

    protected override void init(SerializedProperty property) {
      base.init(property);

      drawProperty("_channelName");
    }
  }
}
