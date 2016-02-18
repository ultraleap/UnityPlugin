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
    public class TrackedQuad : IDisposable
    {
        float _width = 0;
        float _height = 0;
        int _resolutionX = 0;
        int _resolutionY = 0;
        bool _visible = false;
        bool _isValid = false;
        Vector _position;
        Matrix _orientation;
        Int64 _id = 0;

        // TODO: revisit dispose code
        // Dispose() is called explicitly by CoreAssets, so adding the stub implementation for now.
        bool _disposed = false;

        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (_disposed)
                return;

            // cleanup
            if (disposing) {
                // Free any managed objects here.
                //
            }

            // Free any unmanaged objects here.
            //
            _disposed = true;
        }

        /**
    * Constructs a new TrackedQuad object. Do not use. Get valid TrackedQuads
    * from a Controller or Frame object.
    * \include TrackedQuad_constructor_controller.txt
    * \include TrackedQuad_constructor_frame.txt
    * @since 2.2.6
    */
        public TrackedQuad ()
        {
        }

        public TrackedQuad (float width,
                            float height,
                            int resolutionX,
                            int resolutionY,
                            bool visible,
                            Vector position,
                            Matrix orientation,
                            Int64 id)
        {
            _width = width;
            _height = height;
            _resolutionX = resolutionX;
            _resolutionY = resolutionY;
            _visible = visible;
            _position = position;
            _orientation = orientation;
            _id = id;

            _isValid = true;
        }

        ~TrackedQuad ()
        {
            Dispose (false);
        }

        /**
    * Compares quad objects for equality.
    * @since 2.2.6
    */
        public bool Equals (TrackedQuad other)
        {
            return this.IsValid && this == other;
        }

        public override string ToString ()
        {
            return "Tracked quad [" + _width + ", " + _height + "] at " + _position;
        }

    public long Id {
            get {
                return _id;
            }
    }
    /**
    * The physical width of the quad display area in millimeters.
    * \include TrackedQuad_width.txt
    * @since 2.2.6
    */
        public float Width {
            get {
                return _width;
            } 
        }

    /**
    * The physical height of the quad display area in millimeters.
    * \include TrackedQuad_height.txt
    * @since 2.2.6
    */
        public float Height {
            get {
                return _height;
            } 
        }

    /**
    * The horizontal resolution of the quad display area in pixels.
    * This value is set in a configuration file. It is not determined dynamically.
    * \include TrackedQuad_resolutionX.txt
    * @since 2.2.6
    */
        public int ResolutionX {
            get {
                return _resolutionX;
            } 
        }

    /**
    * The vertical resolution of the quad display area in pixels.
    * This value is set in a configuration file. It is not determined dynamically.
    * \include TrackedQuad_resolutionY.txt
    * @since 2.2.6
    */
        public int ResolutionY {
            get {
                return _resolutionY;
            } 
        }

    /**
    * Reports whether the quad is currently detected within the Leap Motion
    * field of view.
    * \include TrackedQuad_visible.txt
    * @since 2.2.6
    */
        public bool Visible {
            get {
                return _visible;
            } 
        }

    /**
    * The orientation of the quad within the Leap Motion frame of reference.
    * \include TrackedQuad_orientation.txt
    * @since 2.2.6
    */
        public Matrix Orientation {
            get {
                    return _orientation;
            } 
        }

    /**
    * The position of the center of the quad display area within the Leap
    * Motion frame of reference. In millimeters.
    * \include TrackedQuad_position.txt
    * @since 2.2.6
    */
        public Vector Position {
            get {
                    return _position;
            } 
        }


    /**
    * The images from which the state of this TrackedQuad was derived.
    * These are the same image objects that you can get from the Controller
    * or Frame object from which you got this TrackedQuad.
    * \include TrackedQuad_images.txt
    * @since 2.2.6
    */
//        public ImageList Images {
//            get {
//                return new ImageList ();
//            } 
//        }

    /**
    * Reports whether this is a valid object.
    * \include TrackedQuad_isValid.txt
    * @since 2.2.6
    */
        public bool IsValid {
            get {
                return _isValid;
            } 
        }

    /**
    * An invalid object.
    * @since 2.2.6
    */
        public static TrackedQuad Invalid {
            get {
                return new TrackedQuad ();
            } 
        }

    }

}
