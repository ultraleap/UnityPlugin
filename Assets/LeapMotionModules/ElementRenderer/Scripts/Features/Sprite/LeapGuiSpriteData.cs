using UnityEngine;
using Leap.Unity.Query;

public static class LeapGuiSpriteExtensions {
  public static LeapGuiSpriteData Sprite(this LeapGuiElement element) {
    return element.data.Query().OfType<LeapGuiSpriteData>().FirstOrDefault();
  }
}

[AddComponentMenu("")]
public class LeapGuiSpriteData : LeapGuiElementData {
  public Sprite sprite;
}
