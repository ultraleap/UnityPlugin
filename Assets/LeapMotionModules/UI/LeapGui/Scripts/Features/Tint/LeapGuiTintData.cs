using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

public static class LeapGuiTintExtensions {
  public static LeapGuiTintData Tint(this LeapGuiElement element) {
    return element.data.Query().FirstOrDefault(d => d is LeapGuiTintData) as LeapGuiTintData;
  }
}

public class LeapGuiTintData : LeapGuiElementData {

  [SerializeField]
  private Color _tint = Color.white;

  public Color tint {
    get {
      return _tint;
    }
    set {
      _tint = value;
    }
  }
}
