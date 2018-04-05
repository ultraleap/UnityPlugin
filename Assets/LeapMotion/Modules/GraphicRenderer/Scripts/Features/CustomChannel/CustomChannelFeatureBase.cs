/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEngine;
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  public interface ICustomChannelFeature {
    string channelName { get; }
  }

  [Serializable]
  public abstract class CustomChannelFeatureBase<T> : LeapGraphicFeature<T>, ICustomChannelFeature
    where T : LeapFeatureData, new() {

    [Tooltip("The name of the channel.  This is the name used to access the channel from within the shader.")]
    [Delayed]
    [EditTimeOnly]
    [SerializeField]
    private string _channelName = "_CustomChannel";

    public string channelName {
      get {
        return _channelName;
      }
    }

    public override SupportInfo GetSupportInfo(LeapGraphicGroup group) {
      foreach (var feature in group.features) {
        if (feature == this) continue;

        var channelFeature = feature as ICustomChannelFeature;
        if (channelFeature != null && channelFeature.channelName == channelName) {
          return SupportInfo.Error("Cannot have two custom channels with the same name.");
        }
      }

      return SupportInfo.FullSupport();
    }
  }
}
