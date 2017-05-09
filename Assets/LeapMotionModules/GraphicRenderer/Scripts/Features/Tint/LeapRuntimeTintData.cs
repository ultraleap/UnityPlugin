/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity.GraphicalRenderer {

  public static class LeapRuntimeTintExtension {
    public static LeapRuntimeTintData GetRuntimeTint(this LeapGraphic graphic) {
      return graphic.GetFeatureData<LeapRuntimeTintData>();
    }
  }

  [LeapGraphicTag("Runtime Tint")]
  [Serializable]
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
