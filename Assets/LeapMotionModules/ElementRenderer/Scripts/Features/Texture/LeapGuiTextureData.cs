using UnityEngine;
using Leap.Unity.Query;

public static class LeapGuiTextureExtensions {
  public static LeapGuiTextureData Texture(this LeapGuiElement element) {
    return element.data.Query().OfType<LeapGuiTextureData>().FirstOrDefault();
  }
}

[AddComponentMenu("")]
public class LeapGuiTextureData : LeapGuiElementData {
  public Texture2D texture;
}
