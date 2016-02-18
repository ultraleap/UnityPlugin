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
   * The InteractionBox class represents a box-shaped region completely
   * within the field of view of the Leap Motion controller.
   *
   * The interaction box is an axis-aligned rectangular prism and provides normalized
   * coordinates for hands, fingers, and tools within this box. The InteractionBox class
   * can make it easier to map positions in the Leap Motion coordinate system to 2D or
   * 3D coordinate systems used for application drawing.
   *
   * \image html images/Leap_InteractionBox.png
   *
   * The InteractionBox region is defined by a center and dimensions along the x, y,
   * and z axes.
   *
   * Get an InteractionBox object from a Frame object.
   * @since 1.0
   */

    public class InteractionBox
    {


        public InteractionBox ()
        {
            Size = Vector.Zero;
            Center = Vector.Zero;
        }

        /** 
         * Create an interaction box with a specxific size and center position.
         * 
         * @param center The midpoint of the box.
         * @param size The dimensions of the box along each axis.
         */
        public InteractionBox (Vector center, Vector size)
        {
            Size = size;
            Center = center;
        }

        /**
     * Normalizes the coordinates of a point using the interaction box.
     *
     * \include InteractionBox_normalizePoint.txt
     *
     * Coordinates from the Leap Motion frame of reference (millimeters) are converted
     * to a range of [0..1] such that the minimum value of the InteractionBox maps to 0
     * and the maximum value of the InteractionBox maps to 1.
     *
     * @param position The input position in device coordinates.
     * @param clamp Whether or not to limit the output value to the range [0,1] when the
     * input position is outside the InteractionBox. Defaults to true.
     * @returns The normalized position.
     * @since 1.0
     */
        public Vector NormalizePoint (Vector position, bool clamp = true)
        {
            if (!this.IsValid) {
                return Vector.Zero;
            }
            float x = (position.x - Center.x + Size.x / 2.0f) / Size.x;
            float y = (position.y - Center.y + Size.y / 2.0f) / Size.y;
            float z = (position.z - Center.z + Size.z / 2.0f) / Size.z;
            if (clamp) {
                x = Math.Min (1.0f, Math.Max (0.0f, x));
                y = Math.Min (1.0f, Math.Max (0.0f, y));
                z = Math.Min (1.0f, Math.Max (0.0f, z));
            }
            return new Vector (x, y, z);
        }

        /**
     * Converts a position defined by normalized InteractionBox coordinates into device
     * coordinates in millimeters.
     *
     * \include InteractionBox_denormalizePoint.txt
     *
     * This function performs the inverse of normalizePoint().
     *
     * @param normalizedPosition The input position in InteractionBox coordinates.
     * @returns The corresponding denormalized position in device coordinates.
     * @since 1.0
     */
        public Vector DenormalizePoint (Vector normalizedPosition)
        {
            if (!IsValid) {
                return Vector.Zero;
            }
            float x = normalizedPosition.x * Size.x + (Center.x - Size.x / 2.0f);
            float y = normalizedPosition.y * Size.y + (Center.y - Size.y / 2.0f);
            float z = normalizedPosition.z * Size.z + (Center.z - Size.z / 2.0f);
            return new Vector (x, y, z);
        }

     /**
     * Compare InteractionBox object equality.
     *
     * \include InteractionBox_operator_equals.txt
     *
     * Two InteractionBox objects are equal if and only if both InteractionBox objects 
     * are the same size, in the same position and both InteractionBoxes are valid.
     * @since 1.0
     */
        public bool Equals (InteractionBox other)
        {
            return this.IsValid && other.IsValid && (this.Center == other.Center) && (this.Size == other.Size);
        }

        /**
     * A string containing a brief, human readable description of the InteractionBox object.
     *
     * @returns A description of the InteractionBox as a string.
     * @since 1.0
     */
        public override string ToString ()
        {
            return "InteractionBox Center: " + Center + ", Size: " + Size;
        }

    /**
     * The center of the InteractionBox in device coordinates (millimeters). This point
     * is equidistant from all sides of the box.
     *
     * \include InteractionBox_center.txt
     *
     * @returns The InteractionBox center in device coordinates.
     * @since 1.0
     */ 
        public Vector Center{ get; set; }

        /** 
         * The dimensions of the interaction box along each axis.
         */
        public Vector Size{ get; set; }

    /**
     * The width of the InteractionBox in millimeters, measured along the x-axis.
     *
     * \include InteractionBox_width.txt
     *
     * @returns The InteractionBox width in millimeters.
     * @since 1.0
     */  
        public float Width {
            get {
                return this.Size.x;
            } 
        }

    /**
     * The height of the InteractionBox in millimeters, measured along the y-axis.
     *
     * \include InteractionBox_height.txt
     *
     * @returns The InteractionBox height in millimeters.
     * @since 1.0
     */  
        public float Height {
            get {
                return this.Size.y;
            } 
        }

    /**
     * The depth of the InteractionBox in millimeters, measured along the z-axis.
     *
     * \include InteractionBox_depth.txt
     *
     * @returns The InteractionBox depth in millimeters.
     * @since 1.0
     */  
        public float Depth {
            get {
                return this.Size.z;
            } 
        }

    /**
     * Reports whether this is a valid InteractionBox object.
     *
     * \include InteractionBox_isValid.txt
     *
     * @returns True, if this InteractionBox object contains valid data.
     * @since 1.0
     */  
        public bool IsValid {
            get {
                return Size != Vector.Zero 
                    && !float.IsNaN (Size.x) 
                    && !float.IsNaN (Size.y) 
                    && !float.IsNaN (Size.z);
            } 
        }

    /**
     * Returns an invalid InteractionBox object.
     *
     * You can use the instance returned by this function in comparisons testing
     * whether a given InteractionBox instance is valid or invalid. (You can also use the
     * InteractionBox::isValid() function.)
     *
     * \include InteractionBox_invalid.txt
     *
     * @returns The invalid InteractionBox instance.
     * @since 1.0
     */  
        public static InteractionBox Invalid {
            get {
                return new InteractionBox ();
            } 
        }

    }

}
