/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEngine.Serialization;
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  public static class LeapSpriteFeatureExtension {
    public static LeapSpriteData Sprite(this LeapGraphic graphic) {
      return graphic.GetFeatureData<LeapSpriteData>();
    }
  }

  [LeapGraphicTag("Sprite")]
  [Serializable]
  public class LeapSpriteData : LeapFeatureData {

    [FormerlySerializedAs("sprite")]
    [EditTimeOnly, SerializeField]
    private Sprite _sprite;

    public Sprite sprite {
      get {
        return _sprite;
      }
      set {
        _sprite = value;
        graphic.isRepresentationDirty = true;
        MarkFeatureDirty();
      }
    }
  }
}
