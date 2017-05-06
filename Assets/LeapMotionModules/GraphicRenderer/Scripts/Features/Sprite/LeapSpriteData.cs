/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEngine;
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  public static class LeapSpriteFeatureExtension {
    public static LeapSpriteData Sprite(this LeapGraphic graphic) {
      return graphic.GetFirstFeatureData<LeapSpriteData>();
    }
  }

  [LeapGraphicTag("Sprite")]
  [Serializable]
  public class LeapSpriteData : LeapFeatureData {

    [EditTimeOnly]
    public Sprite sprite;
  }
}
