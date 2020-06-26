/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
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
