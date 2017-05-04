using System;
using UnityEngine;

namespace Leap.Unity.GraphicalRenderer {

  [LeapGraphicTag("Blend Shape")]
  [Serializable]
  public class LeapBlendShapeFeature : LeapGraphicFeature<LeapBlendShapeData> {
    public const string FEATURE_NAME = LeapGraphicRenderer.FEATURE_PREFIX + "BLEND_SHAPES";

    public override SupportInfo GetSupportInfo(LeapGraphicGroup group) {
      if (!group.renderingMethod.IsValidGraphic<LeapMeshGraphicBase>()) {
        return SupportInfo.Error("Blend shapes require a renderer that supports mesh graphics.");
      } else {
        return SupportInfo.FullSupport();
      }
    }
  }
}
