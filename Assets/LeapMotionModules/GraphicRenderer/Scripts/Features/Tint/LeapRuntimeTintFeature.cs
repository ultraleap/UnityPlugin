using System;
using UnityEngine;

namespace Leap.Unity.GraphicalRenderer {

  [LeapGraphicTag("Tint")]
  [Serializable]
  public class LeapRuntimeTintFeature : LeapGraphicFeature<LeapRuntimeTintData> {
    public const string FEATURE_NAME = LeapGraphicRenderer.FEATURE_PREFIX + "TINTING";
  }
}
