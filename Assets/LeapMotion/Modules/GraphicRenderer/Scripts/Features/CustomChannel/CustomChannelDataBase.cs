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

namespace Leap.Unity.GraphicalRenderer {

  public partial class LeapGraphic {

    /// <summary>
    /// Helper method to get a custom channel data object given the name of the
    /// feature it is attached to.  This method can only be used if the graphic
    /// is currently attached to a group.
    /// </summary>
    public T GetCustomChannel<T>(string channelName) where T : CustomChannelDataBase {
      if (!isAttachedToGroup) {
        throw new Exception("Cannot get a custom channel by name if the graphic is not attached to a group.");
      }

      int index = -1;
      for (int i = 0; i < _featureData.Count; i++) {
        var feature = _featureData[i].feature as ICustomChannelFeature;
        if (feature == null) {
          continue;
        }

        if (feature.channelName == channelName) {
          index = i;
          break;
        }
      }

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
        if (!value.Equals(_value)) {
          MarkFeatureDirty();
          _value = value;
        }
      }
    }
  }
}
