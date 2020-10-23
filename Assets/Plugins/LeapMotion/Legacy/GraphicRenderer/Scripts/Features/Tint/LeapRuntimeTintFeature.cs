/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;

namespace Leap.Unity.GraphicalRenderer {

  [LeapGraphicTag("Runtime Tint", 30)]
  [Serializable]
  public class LeapRuntimeTintFeature : LeapGraphicFeature<LeapRuntimeTintData> {
    public const string FEATURE_NAME = LeapGraphicRenderer.FEATURE_PREFIX + "TINTING";
  }
}
