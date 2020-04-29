/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;

namespace Leap.Unity.GraphicalRenderer {

  [LeapGraphicTag("Custom Channel/Float", 110)]
  [Serializable]
  public class CustomFloatChannelFeature : CustomChannelFeatureBase<CustomFloatChannelData> { }
}
