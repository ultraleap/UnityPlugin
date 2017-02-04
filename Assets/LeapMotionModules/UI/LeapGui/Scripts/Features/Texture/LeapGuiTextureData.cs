using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

public static class LeapGuiTextureExtensions {
  public static LeapGuiTextureData Texture(this LeapGuiElement element) {
    return element.data.Query().FirstOrDefault(d => d is LeapGuiTextureData) as LeapGuiTextureData;
  }
}

[AddComponentMenu("")]
public class LeapGuiTextureData : LeapGuiElementData {
  public Texture2D texture;
}
