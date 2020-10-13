/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity.GraphicalRenderer {

  [LeapGraphicTag("Blend Shape", 40)]
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
