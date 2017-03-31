using UnityEngine;
using Leap.Unity.Attributes;

public static class LeapTextureFeatureExtension {
  public static LeapTextureData Texture(this LeapGraphic graphic) {
    return graphic.GetFirstFeatureData<LeapTextureData>();
  }
}

[LeapGraphicTag("Texture")]
[AddComponentMenu("")]
public class LeapTextureData : LeapFeatureData {

  [EditTimeOnly]
  public Texture2D texture;
}
