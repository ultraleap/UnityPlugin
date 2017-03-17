using UnityEngine;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

public static class LeapGuiSpriteExtensions {
  public static LeapGuiSpriteData Sprite(this LeapGuiElement element) {
    return element.data.Query().OfType<LeapGuiSpriteData>().FirstOrDefault();
  }
}

[AddComponentMenu("")]
public class LeapGuiSpriteData : LeapGuiElementData {

  [EditTimeOnly]
  public Sprite sprite;
}
