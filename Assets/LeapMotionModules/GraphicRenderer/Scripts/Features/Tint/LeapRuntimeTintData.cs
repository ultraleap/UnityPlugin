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

  public partial class LeapGraphic {

    /// <summary>
    /// Helper method to set the runtime tint color for a runtime
    /// tint data object attached to this graphic.  This methof will
    /// throw an exception if there is no blend shape data obj attached
    /// to this graphic.
    /// </summary>
    public void SetRuntimeTint(Color color) {
      getFeatureDataOrThrow<LeapRuntimeTintData>().color = color;
    }
  }

  [LeapGraphicTag("Runtime Tint")]
  [Serializable]
  public class LeapRuntimeTintData : LeapFeatureData {

    [SerializeField]
    private Color _color = Color.white;

    /// <summary>
    /// The runtime tint color for this tint data object.  This
    /// represents a multiplicative tint of the graphic representation
    /// of this graphic.
    /// </summary>
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
