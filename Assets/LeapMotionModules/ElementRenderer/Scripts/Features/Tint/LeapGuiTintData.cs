using UnityEngine;
using Leap.Unity.Query;

public static class LeapGuiTintExtensions {
  public static LeapGuiTintData Tint(this LeapGuiElement element) {
    return element.data.Query().OfType<LeapGuiTintData>().FirstOrDefault();
  }
}

[LeapGuiTag("Tint")]
[AddComponentMenu("")]
public class LeapGuiTintData : LeapGuiElementData {

  [SerializeField]
  private Color _tint = Color.white;

  public Color tint {
    get {
      return _tint;
    }
    set {
      MarkFeatureDirty();
      _tint = value;
    }
  }
}
