/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;

namespace Leap.Unity.GraphicalRenderer {

  public partial class LeapGraphic {

    /// <summary>
    /// Helper method to set the custom channel value for the given channel
    /// name.  This method will throw an exception if there is no channel
    /// with the given name, if the graphic is not currently attached to a
    /// group, or if the channel does not match up with the data type.
    /// </summary>
    public void SetCustomChannel(string channelName, float value) {
      GetCustomChannel<CustomFloatChannelData>(channelName).value = value;
    }
  }

  [LeapGraphicTag("Float Channel")]
  [Serializable]
  public class CustomFloatChannelData : CustomChannelDataBase<float> { }
}
