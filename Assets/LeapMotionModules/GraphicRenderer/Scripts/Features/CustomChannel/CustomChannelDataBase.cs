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
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  public partial class LeapGraphic {

    private T getCustomChannelFeature<T>(string channelName) where T : CustomChannelDataBase {
      int index = _featureData.Query().
                               Select(d => d.feature).
                               Cast<ICustomChannelFeature>().
                               IndexOf(f => f.channelName == channelName);

      if (index == -1) {
        throw new Exception("No custom channel of the name " + channelName + " could be found.");
      }

      T featureDataObj = _featureData[index] as T;
      if (featureDataObj == null) {
        throw new Exception("The channel name " + channelName + " did not match to a custom channel of type " + typeof(T).Name + ".");
      }

      return featureDataObj;
    }
  }

  public abstract class CustomChannelDataBase : LeapFeatureData { }

  public abstract class CustomChannelDataBase<T> : CustomChannelDataBase {

    [SerializeField]
    private T _value;

    public T value {
      get {
        return _value;
      }
      set {
        MarkFeatureDirty();
        _value = value;
      }
    }
  }
}
