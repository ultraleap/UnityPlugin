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
    using LeapInternal;

    /**
   * The Image class represents a single image from one of the Leap Motion cameras.
   *
   * In addition to image data, the Image object provides a distortion map for correcting
   * lens distortion.
   *
   * \include Image_raw.txt
   *
   * Note that Image objects can be invalid, which means that they do not contain
   * valid image data. Get valid Image objects from Frame::frames(). Test for
   * validity with the Image::isValid() function.
   * @since 2.1.0
   */

    public class Image :IDisposable
    {
        private ImageData imageData; //The pooled object containing the actual data
        private UInt64 referenceIndex = 0; //Corresponds to the index in the pooled object

        bool _disposed = false;

        public void Dispose(){
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing){
            if (_disposed)
              return;

            // cleanup
            if (disposing) {
                this.imageData.CheckIn();
            }

            // Free any unmanaged objects here.
            //
            _disposed = true;
        }

        public Image(ImageData data){
            this.imageData = data;
            this.referenceIndex = data.index; //validates that image data hasn't been recycled
        }

        ~Image() {
            Dispose(false);
        }

        public bool IsComplete{
            get{
                return imageData.isComplete;
            }
        }
    /**
  * The image data.
  *
  * The image data is a set of 8-bit intensity values. The buffer is
  * ``image.Width * image.Height * image.BytesPerPixel`` bytes long.
  *
  * \include Image_data_1.txt
  *
  * @since 2.1.0
  */
        public byte[] Data {
            get {
                if(IsValid && imageData.isComplete)
                    return imageData.pixelBuffer;
                return null;
            }
        }

        public void DataWithArg (byte[] dst)
        {
            if(IsValid && imageData.isComplete)
                Buffer.BlockCopy(Data, 0, dst, 0, Data.Length);
        }
        /**
  * The distortion calibration map for this image.
  *
  * The calibration map is a 64x64 grid of points. Each point is defined by
  * a pair of 32-bit floating point values. Each point in the map
  * represents a ray projected into the camera. The value of
  * a grid point defines the pixel in the image data containing the brightness
  * value produced by the light entering along the corresponding ray. By
  * interpolating between grid data points, you can find the brightness value
  * for any projected ray. Grid values that fall outside the range [0..1] do
  * not correspond to a value in the image data and those points should be ignored.
  *
  * \include Image_distortion_1.txt
  *
  * The calibration map can be used to render an undistorted image as well as to
  * find the true angle from the camera to a feature in the raw image. The
  * distortion map itself is designed to be used with GLSL shader programs.
  * In other contexts, it may be more convenient to use the Image Rectify()
  * and Warp() functions.
  *
  * Distortion is caused by the lens geometry as well as imperfections in the
  * lens and sensor window. The calibration map is created by the calibration
  * process run for each device at the factory (and which can be rerun by the
  * user).
  *
  * Note, in a future release, there will be two distortion maps per image;
  * one containing the horizontal values and the other containing the vertical values.
  *
  * @since 2.1.0
  */
        public float[] Distortion {
            get {
                if(IsValid && imageData.isComplete)
                    return imageData.DistortionData.Data;

                return new float[0];
            }
        }

        public void DistortionWithArg (float[] dst)
        {
            if(IsValid && imageData.isComplete)
                Buffer.BlockCopy(Distortion, 0, dst, 0, Distortion.Length);
        }

        /**
     * Constructs a Image object.
     *
     * An uninitialized image is considered invalid.
     * Get valid Image objects from a ImageList object obtained from the
     * Frame::images() method.
     *
     *
     * @since 2.1.0
     */
        public Image ()
        {
        }

        /**
     * Provides the corrected camera ray intercepting the specified point on the image.
     *
     * Given a point on the image, ``rectify()`` corrects for camera distortion
     * and returns the true direction from the camera to the source of that image point
     * within the Leap Motion field of view.
     *
     * This direction vector has an x and y component [x, y, 0], with the third element
     * always zero. Note that this vector uses the 2D camera coordinate system
     * where the x-axis parallels the longer (typically horizontal) dimension and
     * the y-axis parallels the shorter (vertical) dimension. The camera coordinate
     * system does not correlate to the 3D Leap Motion coordinate system.
     *
     * \include Image_rectify_1.txt
     *
     * @param uv A Vector containing the position of a pixel in the image.
     * @returns A Vector containing the ray direction (the z-component of the vector is always 0).
     * @since 2.1.0
     */
        public Vector Rectify (Vector uv)
        {
            if(this.IsValid && imageData.isComplete){
                //TODO update Rectify to deal with stacked images and distortion
                //Warp uv to correct distortion
                Vector rectified = Warp(uv);
                //normalize to ray
                rectified.x = (rectified.x/Width - RayOffsetX) / RayScaleX ;
                rectified.y = (rectified.y/Height - RayOffsetY) / RayScaleY ;
                return rectified;
            }
            return Vector.Zero;
        }

        /**
     * Provides the point in the image corresponding to a ray projecting
     * from the camera.
     *
     * Given a ray projected from the camera in the specified direction, ``warp()``
     * corrects for camera distortion and returns the corresponding pixel
     * coordinates in the image.
     *
     * The ray direction is specified in relationship to the camera. The first
     * vector element corresponds to the "horizontal" view angle; the second
     * corresponds to the "vertical" view angle.
     *
     * \include Image_warp_1.txt
     *
     * The ``warp()`` function returns pixel coordinates outside of the image bounds
     * if you project a ray toward a point for which there is no recorded data.
     *
     * ``warp()`` is typically not fast enough for realtime distortion correction.
     * For better performance, use a shader program exectued on a GPU.
     *
     * @param xy A Vector containing the ray direction.
     * @returns A Vector containing the pixel coordinates [x, y, 0] (with z always zero).
     * @since 2.1.0
     */
        public Vector Warp (Vector xy)
        {
            //TODO Need to update Warp to deal with stacked images and distortion
            if(this.IsValid && imageData.isComplete)
                return Warp(xy, imageData.width, imageData.height);
            return Vector.Zero;
        }

        /**
         * Finds the undistorted brightness for a pixel in a target image.
         * The brightness is scaled and undistorted using bilinear interpolation.
         * @param xy A Vector containing the ray direction.
         * @param targetWidth the width of the destination image.
         * @param targtHeight the height of the destination image.
         * @returns A Vector containing the pixel coordinates [x, y, 0] (with z always zero).
         */
        public Vector Warp (Vector xy, float targetWidth, float targetHeight)
        {
            if(this.IsValid && imageData.isComplete){
                //Calculate the position in the calibration map (still with a fractional part)
                float calibrationX = 63 * xy.x / targetWidth;
                float calibrationY = 62 * (1 - xy.y / targetHeight); // The y origin is at the bottom
                //Save the fractional part to use as the weight for interpolation
                float weightX = calibrationX - (int)calibrationX;
                float weightY = calibrationY - (int)calibrationY;
                        
                //Get the integer x,y coordinates of the closest calibration map points to the target pixel
                int x1 = (int)calibrationX; //Note truncation to int
                int y1 = (int)calibrationY;
                int x2 = x1 + 1;
                int y2 = y1 + 1;
                        
                //Look up the x and y values for the 4 calibration map points around the target
                float dX1 = Distortion [x1 * 2 + y1 * DistortionWidth];
                float dX2 = Distortion [x2 * 2 + y1 * DistortionWidth];
                float dX3 = Distortion [x1 * 2 + y2 * DistortionWidth];
                float dX4 = Distortion [x2 * 2 + y2 * DistortionWidth];
                float dY1 = Distortion [x1 * 2 + y1 * DistortionWidth + 1];
                float dY2 = Distortion [x2 * 2 + y1 * DistortionWidth + 1];
                float dY3 = Distortion [x1 * 2 + y2 * DistortionWidth + 1];
                float dY4 = Distortion [x2 * 2 + y2 * DistortionWidth + 1];
                        
                //Bilinear interpolation of the looked-up values:
                // X value
                float dX = dX1 * (1 - weightX) * (1 - weightY) +
                           dX2 * weightX * (1 - weightY) +
                           dX3 * (1 - weightX) * weightY +
                           dX4 * weightX * weightY;
                        
                // Y value
                float dY = dY1 * (1 - weightX) * (1 - weightY) +
                           dY2 * weightX * (1 - weightY) +
                           dY3 * (1 - weightX) * weightY +
                           dY4 * weightX * weightY;
                        
                return new Vector (dX * Width, dY * Height, 0);
            }
            return Vector.Zero;
        }
        /**
     * Compare Image object equality.
     *
     * Two Image objects are equal if and only if both Image objects represent the
     * exact same Image and both Images are valid.
     * @since 2.1.0
     */
        public bool Equals (Image other)
        {
            return this.IsValid &&
                other.IsValid &&
                this.SequenceId == other.SequenceId &&
                this.Type == other.Type &&
                this.Timestamp == other.Timestamp;
        }

        /**
     * A string containing a brief, human readable description of the Image object.
     *
     * @returns A description of the Image as a string.
     * @since 2.1.0
     */
        public override string ToString ()
        {
            if(this.IsValid && imageData.isComplete)
                return "Image sequence" + this.SequenceId + ", format: " + this.Format + ", type: " + this.Type;

            if(this.IsValid)
                return "Incomplete image sequence" + this.SequenceId + ", format: " + this.Format + ", type: " + this.Type;
            
            return "Invalid Image";
        }

/**
     * The image sequence ID.
     *
     * \include Image_sequenceId.txt
     *
     * @since 2.2.1
     */
        public long SequenceId {
            get {
                if(IsValid)
                    return (long)this.imageData.frame_id;

                return -1;
            } 
        }


/**
     * The image width.
     *
     * \include Image_image_width_1.txt
     *
     * @since 2.1.0
     */
        public int Width {
            get {
                if(IsValid)
                    return (int)imageData.width;

                return 0;
            } 
        }

/**
     * The image height.
     *
     * \include Image_image_height_1.txt
     *
     * @since 2.1.0
     */
        public int Height {
            get {
                if(IsValid)
                    return (int)imageData.height;

                return 0;
            } 
        }

/**
     * The number of bytes per pixel.
     *
     * Use this value along with ``Image::width()`` and ``Image:::height()``
     * to calculate the size of the data buffer.
     *
     * \include Image_bytesPerPixel.txt
     *
     * @since 2.2.0
     */
        public int BytesPerPixel {
            get {
                if(IsValid)
                    return (int)imageData.bpp;

                return 1;
            } 
        }

/**
     * The image format.
     *
     * \include Image_format.txt
     *
     * @since 2.2.0
     */
        public Image.FormatType Format {
            get {
                if(IsValid){
                    switch (imageData.format) {
                    case eLeapImageFormat.eLeapImageType_IR:
                        return Image.FormatType.INFRARED;
                    case eLeapImageFormat.eLeapImageType_RGBIr_Bayer:
                        return Image.FormatType.IBRG;
                    default:
                        return Image.FormatType.INFRARED;
                    }
                }
                return Image.FormatType.INFRARED;
            } 
        }

        public Image.ImageType Type{
            get{
                if(IsValid){
                    switch (imageData.type) {
                    case eLeapImageType.eLeapImageType_Default:
                        return Image.ImageType.DEFAULT;
                    case eLeapImageType.eLeapImageType_Raw:
                        return Image.ImageType.RAW;
                    default:
                        return Image.ImageType.DEFAULT;
                    }
                }
                return Image.ImageType.DEFAULT;
            }
        }

/**
     * The stride of the distortion map.
     *
     * Since each point on the 64x64 element distortion map has two values in the
     * buffer, the stride is 2 times the size of the grid. (Stride is currently fixed
     * at 2 * 64 = 128).
     *
     * \include Image_distortion_width_1.txt
     *
     * @since 2.1.0
     */
        public int DistortionWidth {
            get {
                if(IsValid && imageData.isComplete)
                    return imageData.DistortionSize * 2;

                return 0;
            } 
        }

/**
     * The distortion map height.
     *
     * Currently fixed at 64.
     *
     * \include Image_distortion_height_1.txt
     *
     * @since 2.1.0
     */
        public int DistortionHeight {
            get {
                if(IsValid && imageData.isComplete)
                    return imageData.DistortionSize;

                return 0;
            } 
        }

/**
     * The horizontal ray offset.
     *
     * Used to convert between normalized coordinates in the range [0..1] and the
     * ray slope range [-4..4].
     *
     * \include Image_ray_factors_1.txt
     *
     * @since 2.1.0
     */
        public float RayOffsetX {
            get {
                if(IsValid && imageData.isComplete)
                    return imageData.RayOffsetX;

                return 0;
            } 
        }

/**
     * The vertical ray offset.
     *
     * Used to convert between normalized coordinates in the range [0..1] and the
     * ray slope range [-4..4].
     *
     * \include Image_ray_factors_2.txt
     *
     * @since 2.1.0
     */
        public float RayOffsetY {
            get {
                if(IsValid && imageData.isComplete)
                    return imageData.RayOffsetY;

                return 0;
            } 
        }

/**
     * The horizontal ray scale factor.
     *
     * Used to convert between normalized coordinates in the range [0..1] and the
     * ray slope range [-4..4].
     *
     * \include Image_ray_factors_1.txt
     *
     * @since 2.1.0
     */
        public float RayScaleX {
            get {
                if(IsValid && imageData.isComplete)
                    return imageData.RayScaleX;

                return 0;
            } 
        }

/**
     * The vertical ray scale factor.
     *
     * Used to convert between normalized coordinates in the range [0..1] and the
     * ray slope range [-4..4].
     *
     * \include Image_ray_factors_2.txt
     *
     * @since 2.1.0
     */
        public float RayScaleY {
            get {
                if(IsValid && imageData.isComplete)
                    return imageData.RayScaleY;

                return 0;
            } 
        }

/**
     * Returns a timestamp indicating when this frame began being captured on the device.
     * 
     * @since 2.2.7
     */
        public long Timestamp {
            get {
                if(IsValid && imageData.isComplete)
                    return (long)imageData.timestamp;

                return 0;
            } 
        }

/**
     * Reports whether this Image instance contains valid data.
     *
     * @returns true, if and only if the image is valid.
     * @since 2.1.0
     */
        public bool IsValid {
            get {
                //If indexes are different, the ImageData object has been reused and is no longer valid for this image
                return (this.imageData != null) && (this.referenceIndex == this.imageData.index); 
            } 
        }

/**
     * Returns an invalid Image object.
     *
     * You can use the instance returned by this function in comparisons testing
     * whether a given Image instance is valid or invalid. (You can also use the
     * Image::isValid() function.)
     *
     * @returns The invalid Image instance.
     * @since 2.1.0
     */
        public static Image Invalid {
            get {
                return new Image ();
            } 
        }

        /**
       * Enumerates the possible image formats.
       *
       * The Image::format() function returns an item from the FormatType enumeration.
       * @since 2.2.0
       */
        public enum FormatType
        {
            INFRARED = 0,
            IBRG = 1
        }
        /**
       * Enumerates the image perspectives.
       *
       * 
       * @since 3.0
       */
        public enum PerspectiveType
        {
            INVALID = 0,  //!< Invalid or unknown image perspective
            STEREO_LEFT = 1, //!< Left side of a stereo pair
            STEREO_RIGHT = 2, //!< Right side of a stereo pair
            MONO = 3 //!< Reserved for future use
        }

        public enum ImageType
        {
            DEFAULT,
            RAW
        }

        public enum RequestFailureReason{
            Image_Unavailable,
            Images_Disabled,
            Insufficient_Buffer,
            Unknown_Error,
        }
    }

}
