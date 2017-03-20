using UnityEngine;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

public static class LeapGuiTextureExtensions {
  public static LeapGuiTextureData Texture(this LeapGuiElement element) {
    return element.data.Query().OfType<LeapGuiTextureData>().FirstOrDefault();
  }
}

[LeapGuiTag("Texture")]
[AddComponentMenu("")]
public class LeapGuiTextureData : LeapGuiElementData {

  [EditTimeOnly]
  public Texture2D texture;
}
