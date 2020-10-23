/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using System;

namespace Leap.Unity.GraphicalRenderer {

  public partial class LeapGraphic {

    /// <summary>
    /// Helper method to set the custom channel value for the given channel
    /// name.  This method will throw an exception if there is no channel
    /// with the given name, if the graphic is not currently attached to a
    /// group, or if the channel does not match up with the data type.
    /// </summary>
    public void SetCustomChannel(string channelName, Matrix4x4 value) {
      GetCustomChannel<CustomMatrixChannelData>(channelName).value = value;
    }
  }

  [LeapGraphicTag("Matrix Channel")]
  [Serializable]
  public class CustomMatrixChannelData : CustomChannelDataBase<Matrix4x4> { }
}
