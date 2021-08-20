/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;

namespace Leap {
  /// <summary>
  /// The DistortionData class contains the distortion map for correcting the
  /// lens distortion of an image.
  ///
  /// The distortion data is an array containing a 64x64 grid of floating point pairs.
  /// The distortion map for both sides of an image pair are stacked in
  /// the Data array -- the left map first, followed by the right map.
  ///
  /// @since 3.0
  /// </summary>
  public class DistortionData {
    /// <summary>
    /// Constructs an uninitialized distortion object.
    /// @since 3.0
    /// </summary>
    public DistortionData() { }

    /// <summary>
    /// @since 3.0
    /// </summary>
    public DistortionData(UInt64 version, float width, float height, float[] data) {
      Version = version;
      Width = width;
      Height = height;
      Data = data;
    }
    /// <summary>
    /// An identifier assigned to the distortion map.
    ///
    /// When the distortion map changes -- either because the devices flips the images
    /// to automatically orient the hands or because a different device is plugged in,
    /// the version number of the distortion data changes.
    ///
    /// Note that the version always increases. If the images change orientation and then
    /// return to their original orientation, a new version number is assigned. Thus
    /// the version number can be used to detect when the data has changed, but not
    /// to uniquely identify the data.
    /// @since 3.0
    /// </summary>
    public UInt64 Version { get; set; }
    /// <summary>
    /// The width of the distortion map.
    ///
    /// Currently always 64. Note that there are two floating point values for every point in the map.
    /// @since 3.0
    /// </summary>
    public float Width { get; set; }
    /// <summary>
    /// The height of the distortion map.
    ///
    /// Currently always 64.
    /// @since 3.0
    /// </summary>
    public float Height { get; set; }
    /// <summary>
    /// The distortion data.
    ///
    /// @since 3.0
    /// </summary>
    public float[] Data { get; set; }
    /// <summary>
    /// Reports whether the distortion data is internally consistent.
    /// @since 3.0
    /// </summary>
    public bool IsValid {
      get {
        if (Data != null &&
            Width == LeapInternal.LeapC.DistortionSize &&
            Height == LeapInternal.LeapC.DistortionSize &&
            Data.Length == Width * Height * 2)
          return true;

        return false;
      }
    }
  }
}
