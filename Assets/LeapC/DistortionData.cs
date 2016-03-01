/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
using System;
using System.Collections.Generic;

namespace Leap
{
  /**
   * The DistortionData class contains the distortion map for correcting the
   * lens distortion of an image.
   *
   * The distortion data is an array containing a 64x64 grid of floating point pairs.
   * The distortion map for both sides of an image pair are stacked in
   * the Data array -- the left map first, followed by the right map.
   *
   * @since 3.0
   */
  public class DistortionData
  {
    /**
     * Constructs an uninitialized distortion object.
     * @since 3.0
     */
    public DistortionData() { }

    /**
     * @since 3.0
     */
    public DistortionData(UInt64 version, float width, float height, float[] data)
    {
      Version = version;
      Width = width;
      Height = height;
      Data = data;
    }
    /**
     * An identifier assigned to the distortion map.
     *
     * When the distortion map changes -- either because the devices flips the images
     * to automatically orient the hands or because a different device is plugged in,
     * the version number of the distortion data changes.
     *
     * Note that the version always increases. If the images change orientation and then
     * return to their original orientation, a new version number is assigned. Thus
     * the version number can be used to detect when the data has changed, but not
     * to uniquely identify the data.
     * @since 3.0
     */
    public UInt64 Version { get; set; }
    /**
     * The width of the distortion map.
     *
     * Currently always 64. Note that there are two floating point values for every point in the map.
     * @since 3.0
     */
    public float Width { get; set; }
    /**
     * The height of the distortion map.
     *
     * Currently always 64.
     * @since 3.0
     */
    public float Height { get; set; }
    /**
     * The distortion data.
     *
     * @since 3.0
     */
    public float[] Data { get; set; }
    /**
     * Reports whether the distortion data is internally consistent.
     * @since 3.0
     */
    public bool IsValid
    {
      get
      {
        if (Data != null &&
            Width == LeapInternal.LeapC.DistortionSize &&
            Height == LeapInternal.LeapC.DistortionSize &&
            Data.Length == Width * Height * 2 * 2)
          return true;

        return false;
      }
    }
  }
}

