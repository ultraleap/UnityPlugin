/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEditor;
using UnityEngine;

namespace Leap.Unity.GraphicalRenderer {

  [CustomPropertyDrawer(typeof(LeapTextureFeature))]
  public class LeapTextureFeatureDrawer : CustomPropertyDrawerBase {

    protected override void init(SerializedProperty property) {
      base.init(property);

      drawProperty("propertyName");
      drawProperty("channel");
    }
  }
}
