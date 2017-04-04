using UnityEngine;

namespace Leap.Unity.GraphicalRenderer {

  public static class LeapRuntimeTintExtension {
    public static LeapRuntimeTintData GetRuntimeTint(this LeapGraphic graphic) {
      return graphic.GetFirstFeatureData<LeapRuntimeTintData>();
    }
  }

  [LeapGraphicTag("Runtime Tint")]
  [AddComponentMenu("")]
  public class LeapRuntimeTintData : LeapFeatureData {

    [SerializeField]
    private Color _color = Color.white;

    public Color color {
      get {
        return _color;
      }
      set {
        MarkFeatureDirty();
        _color = value;
      }
    }
  }
}
