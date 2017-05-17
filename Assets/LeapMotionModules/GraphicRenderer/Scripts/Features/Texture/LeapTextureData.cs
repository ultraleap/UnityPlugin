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
