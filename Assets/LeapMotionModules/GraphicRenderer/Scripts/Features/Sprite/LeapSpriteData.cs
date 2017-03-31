using UnityEngine;
using Leap.Unity.Attributes;

public static class LeapSpriteFeatureExtension {
  public static LeapSpriteData Sprite(this LeapGraphic graphic) {
    return graphic.GetFirstFeatureData<LeapSpriteData>();
  }
}

[LeapGraphicTag("Sprite")]
[AddComponentMenu("")]
public class LeapSpriteData : LeapFeatureData {

  [EditTimeOnly]
  public Sprite sprite;
}
