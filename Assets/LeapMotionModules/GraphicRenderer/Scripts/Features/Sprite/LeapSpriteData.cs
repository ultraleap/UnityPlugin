using System;
using UnityEngine;
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  public static class LeapSpriteFeatureExtension {
    public static LeapSpriteData Sprite(this LeapGraphic graphic) {
      return graphic.GetFirstFeatureData<LeapSpriteData>();
    }
  }

  [LeapGraphicTag("Sprite")]
  [Serializable]
  public class LeapSpriteData : LeapFeatureData {

    [EditTimeOnly]
    public Sprite sprite;
  }
}
