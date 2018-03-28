/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;

namespace Leap.Unity.GraphicalRenderer {

  [LeapGraphicTag("Custom Channel/Float", 110)]
  [Serializable]
  public class CustomFloatChannelFeature : CustomChannelFeatureBase<CustomFloatChannelData> { }
}
