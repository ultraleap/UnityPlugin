/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.GraphicalRenderer {

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
