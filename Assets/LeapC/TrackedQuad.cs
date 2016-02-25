/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
namespace Leap
{
  using System;
  using System.Runtime.InteropServices;

  /**
   * Note: This class is an experimental API for internal use only. It may be
   * removed without warning.
   *
   * Represents a quad-like object tracked by the Leap Motion sensors.
   *
   * Only one quad can be tracked. Once a supported quad is tracked, the state
   * of that quad will be updated for each frame of Leap Motion tracking data.
   *
   * A TrackedQuad object represents the state of the quad at one moment in time.
   * Get a new object from subsequent frames to get the latest state information.
   * @since 2.2.6
   */
  public class TrackedQuad
  {
    /**
     * Constructs a default TrackedQuad object
     * \include TrackedQuad_constructor_controller.txt
     * \include TrackedQuad_constructor_frame.txt
     * @since 2.2.6
     */
    public TrackedQuad() {}

    public TrackedQuad(float width,
                        float height,
                        int resolutionX,
                        int resolutionY,
                        bool visible,
                        Vector position,
                        Matrix orientation,
                        Int64 id)
    {
      Width = width;
      Height = height;
      ResolutionX = resolutionX;
      ResolutionY = resolutionY;
      Visible = visible;
      Position = position;
      Orientation = orientation;
      Id = id;
    }

    /**
     * Compares quad objects for equality.
     * @since 2.2.6
     */
    public bool Equals(TrackedQuad other)
    {
      return this == other;
    }

    public override string ToString()
    {
      return "Tracked quad [" + Width + ", " + Height + "] at " + Position;
    }

    public long Id { get; private set; }

    /**
     * The physical width of the quad display area in millimeters.
     * \include TrackedQuad_width.txt
     * @since 2.2.6
     */
    public float Width { get; private set; }

    /**
     * The physical height of the quad display area in millimeters.
     * \include TrackedQuad_height.txt
     * @since 2.2.6
     */
    public float Height { get; private set; }

    /**
     * The horizontal resolution of the quad display area in pixels.
     * This value is set in a configuration file. It is not determined dynamically.
     * \include TrackedQuad_resolutionX.txt
     * @since 2.2.6
     */
    public int ResolutionX { get; private set; }

    /**
     * The vertical resolution of the quad display area in pixels.
     * This value is set in a configuration file. It is not determined dynamically.
     * \include TrackedQuad_resolutionY.txt
     * @since 2.2.6
     */
    public int ResolutionY { get; private set; }

    /**
     * Reports whether the quad is currently detected within the Leap Motion
     * field of view.
     * \include TrackedQuad_visible.txt
     * @since 2.2.6
     */
    public bool Visible { get; private set; }

    /**
     * The orientation of the quad within the Leap Motion frame of reference.
     * \include TrackedQuad_orientation.txt
     * @since 2.2.6
     */
    public Matrix Orientation { get; private set; }

    /**
     * The position of the center of the quad display area within the Leap
     * Motion frame of reference. In millimeters.
     * \include TrackedQuad_position.txt
     * @since 2.2.6
     */
    public Vector Position { get; private set; }
  }
}
