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
    /// tint data object attached to this graphic.  This method will
    /// throw an exception if there is no tint data obj attached
    /// to this graphic.
    /// </summary>
    public void SetRuntimeTint(Color color) {
      getFeatureDataOrThrow<LeapRuntimeTintData>().color = color;
    }

    /// <summary>
    /// Overload of SetRuntimeTint that takes in a Html style string
    /// code that represents a color.  Any string that can be parsed
    /// by ColorUtility.TryParseHtmlString can be used as an argument
    /// to this method.
    /// </summary>
    public void SetRuntimeTint(string htmlString) {
      SetRuntimeTint(Utils.ParseHtmlColorString(htmlString));
    }

    /// <summary>
    /// Helper method to get the runtime tint color for a runtime 
    /// tint data object attached to this graphic.  This method will
    /// throw an exception if there is no tint data obj attached to 
    /// this graphic.
    /// </summary>
    public Color GetRuntimeTint() {
      return getFeatureDataOrThrow<LeapRuntimeTintData>().color;
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
        if (value != _color) {
          MarkFeatureDirty();
          _color = value;
        }
      }
    }
  }
}
