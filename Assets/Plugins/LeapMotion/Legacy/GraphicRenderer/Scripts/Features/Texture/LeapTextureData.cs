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

  public static class LeapTextureFeatureExtension {
    public static LeapTextureData Texture(this LeapGraphic graphic) {
      return graphic.GetFeatureData<LeapTextureData>();
    }
  }

  [LeapGraphicTag("Texture")]
  [Serializable]
  public class LeapTextureData : LeapFeatureData {

    [FormerlySerializedAs("texture")]
    [EditTimeOnly, SerializeField]
    private Texture2D _texture;

    public Texture2D texture {
      get {
        return _texture;
      }
      set {
        _texture = value;
        graphic.isRepresentationDirty = true;
        MarkFeatureDirty();
      }
    }
  }
}
