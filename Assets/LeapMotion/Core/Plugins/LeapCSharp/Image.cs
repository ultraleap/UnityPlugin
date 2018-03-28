/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace Leap {
  using System;
  using System.Runtime.InteropServices;
  using LeapInternal;

  /**
   * The Image class represents a stereo image pair from the Leap Motion device.
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

  public class Image {
    private ImageData leftImage;
    private ImageData rightImage;
    private Int64 frameId = 0;
    private Int64 timestamp = 0;

    public Image(Int64 frameId, Int64 timestamp, ImageData leftImage, ImageData rightImage) {
      if (leftImage == null || rightImage == null) {
        throw new ArgumentNullException("images");
      }
      if (leftImage.type != rightImage.type ||
         leftImage.format != rightImage.format ||
         leftImage.width != rightImage.width ||
         leftImage.height != rightImage.height ||
         leftImage.bpp != rightImage.bpp ||
         leftImage.DistortionSize != rightImage.DistortionSize) {
        throw new ArgumentException("image mismatch");
      }
      this.frameId = frameId;
      this.timestamp = timestamp;
      this.leftImage = leftImage;
      this.rightImage = rightImage;
    }

    private ImageData imageData(CameraType camera) {
      return camera == CameraType.LEFT ? leftImage : rightImage;
    }

    /**
     * The buffer containing the image data.
     *
     * The image data is a set of 8-bit intensity values. The buffer is
     * ``image.Width * image.Height * image.BytesPerPixel`` bytes long.
     *
     * Use the ``ByteOffset()`` method to find the beginning offset
     * of the data for the specified camera.
     *
     * \include Image_data_1.txt
     *
     * @since 4.0
     */
    public byte[] Data(CameraType camera) {
      if (camera != CameraType.LEFT && camera != CameraType.RIGHT)
        return null;

      return imageData(camera).AsByteArray;
    }

    /**
     * The offset, in number of bytes, from the beginning of the ``Data()``
     * buffer to the first byte of the image data for the specified camera.
     *
     * \include Image_data_1.txt
     *
     * @since 4.0
     */
    public UInt32 ByteOffset(CameraType camera) {
      if (camera != CameraType.LEFT && camera != CameraType.RIGHT)
        return 0;

      return imageData(camera).byteOffset;
    }

    /**
     * The number of bytes in the ``Data()`` buffer corresponding to each
     * image. Use the ``ByteOffset()`` function to find the starting byte
     * offset for each image.
     *
     * \include Image_data_1.txt
     *
     * @since 4.0
     */
    public UInt32 NumBytes {
      get {
        return leftImage.width * leftImage.height * leftImage.bpp;
      }
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
    public float[] Distortion(CameraType camera) {
      if (camera != CameraType.LEFT && camera != CameraType.RIGHT)
        return null;

      return imageData(camera).DistortionData.Data;
    }

    /**
     * Provides the corrected camera ray intercepting the specified point on the image.
     *
     * Given a point on the image, ``PixelToRectilinear()`` corrects for camera distortion
     * and returns the true direction from the camera to the source of that image point
     * within the Leap Motion field of view.
     *
     * This direction vector has an x and y component [x, y, 1], with the third element
     * always one. Note that this vector uses the 2D camera coordinate system
     * where the x-axis parallels the longer (typically horizontal) dimension and
     * the y-axis parallels the shorter (vertical) dimension. The camera coordinate
     * system does not correlate to the 3D Leap Motion coordinate system.
     *
     * \include Image_rectify_1.txt
     *
     * **Note:** This function should be called immediately after an image is obtained. Incorrect
     * results will be returned if the image orientation has changed or a different device is plugged
     * in between the time the image was received and the time this function is called.
     *
     * Note, this function was formerly named Rectify().
     *
     * @param camera whether the pixel parameter is a pixel in the left or the right stereo image.
     * @param pixel A Vector containing the position of a pixel in the image.
     * @returns A Vector containing the ray direction (the z-component of the vector is always one).
     * @since 2.1.0
     */
    public Vector PixelToRectilinear(CameraType camera, Vector pixel) {
      return Connection.GetConnection().PixelToRectilinear(camera, pixel);
    }

    /**
     * Provides the point in the image corresponding to a ray projecting
     * from the camera.
     *
     * Given a ray projected from the camera in the specified direction, ``RectilinearToPixel()``
     * corrects for camera distortion and returns the corresponding pixel
     * coordinates in the image.
     *
     * The ray direction is specified in relationship to the camera. The first
     * vector element corresponds to the "horizontal" view angle; the second
     * corresponds to the "vertical" view angle.
     *
     * \include Image_warp_1.txt
     *
     * The ``RectilinearToPixel()`` function returns pixel coordinates outside of the image bounds
     * if you project a ray toward a point for which there is no recorded data.
     *
     * ``RectilinearToPixel()`` is typically not fast enough for realtime distortion correction.
     * For better performance, use a shader program executed on a GPU.
     *
     * **Note:** This function should be called immediately after an image is obtained. Incorrect
     * results will be returned if the image orientation has changed or a different device is plugged
     * in between the time the image was received and the time this function is called.
     *
     * Note, this function was formerly named Warp().
     *
     * @param camera whether the ray parameter intercepts the left or the right stereo image.
     * @param ray A Vector containing the ray direction.
     * @returns A Vector containing the pixel coordinates [x, y, 1] (with z always one).
     * @since 2.1.0
     */
    public Vector RectilinearToPixel(CameraType camera, Vector ray) {
      return Connection.GetConnection().RectilinearToPixel(camera, ray);
    }

    /**
     * Compare Image object equality.
     *
     * Two Image objects are equal if and only if both Image objects represent the
     * exact same Image and both Images are valid.
     * @since 2.1.0
     */
    public bool Equals(Image other) {
      return
          this.frameId == other.frameId &&
          this.Type == other.Type &&
          this.Timestamp == other.Timestamp;
    }

    /**
     * A string containing a brief, human readable description of the Image object.
     *
     * @returns A description of the Image as a string.
     * @since 2.1.0
     */
    public override string ToString() {
      return "Image sequence" + this.frameId + ", format: " + this.Format + ", type: " + this.Type;
    }

    /**
     * The image sequence ID.
     *
     * \include Image_sequenceId.txt
     *
     * @since 2.2.1
     */
    public Int64 SequenceId {
      get {
        return frameId;
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
        return (int)leftImage.width;
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
        return (int)leftImage.height;
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
        return (int)leftImage.bpp;
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
        switch (leftImage.format) {
          case eLeapImageFormat.eLeapImageType_IR:
            return Image.FormatType.INFRARED;
          case eLeapImageFormat.eLeapImageType_RGBIr_Bayer:
            return Image.FormatType.IBRG;
          default:
            return Image.FormatType.INFRARED;
        }
      }
    }

    public Image.ImageType Type {
      get {
        switch (leftImage.type) {
          case eLeapImageType.eLeapImageType_Default:
            return Image.ImageType.DEFAULT;
          case eLeapImageType.eLeapImageType_Raw:
            return Image.ImageType.RAW;
          default:
            return Image.ImageType.DEFAULT;
        }
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
        return leftImage.DistortionSize * 2;
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
        return leftImage.DistortionSize;
      }
    }

    /**
     * The horizontal ray offset for a particular camera.
     *
     * Used to convert between normalized coordinates in the range [0..1] and the
     * ray slope range [-4..4].
     *
     * \include Image_ray_factors_1.txt
     *
     * @since 4.0
     */
    public float RayOffsetX(CameraType camera) {
      if (camera != CameraType.LEFT && camera != CameraType.RIGHT)
        return 0;

      return imageData(camera).RayOffsetX;
    }

    /**
     * The vertical ray offset for a particular camera.
     *
     * Used to convert between normalized coordinates in the range [0..1] and the
     * ray slope range [-4..4].
     *
     * \include Image_ray_factors_2.txt
     *
     * @since 2.1.0
     */
    public float RayOffsetY(CameraType camera) {
      if (camera != CameraType.LEFT && camera != CameraType.RIGHT)
        return 0;

      return imageData(camera).RayOffsetY;
    }

    /**
     * The horizontal ray scale factor for a particular camera.
     *
     * Used to convert between normalized coordinates in the range [0..1] and the
     * ray slope range [-4..4].
     *
     * \include Image_ray_factors_1.txt
     *
     * @since 2.1.0
     */
    public float RayScaleX(CameraType camera) {
      if (camera != CameraType.LEFT && camera != CameraType.RIGHT)
        return 0;

      return imageData(camera).RayScaleX;
    }

    /**
     * The vertical ray scale factor for a particular camera.
     *
     * Used to convert between normalized coordinates in the range [0..1] and the
     * ray slope range [-4..4].
     *
     * \include Image_ray_factors_2.txt
     *
     * @since 2.1.0
     */
    public float RayScaleY(CameraType camera) {
      if (camera != CameraType.LEFT && camera != CameraType.RIGHT)
        return 0;

      return imageData(camera).RayScaleY;
    }

    /**
     * Returns a timestamp indicating when this frame began being captured on the device.
     *
     * @since 2.2.7
     */
    public Int64 Timestamp {
      get {
        return timestamp;
      }
    }

    /**
     * Enumerates the possible image formats.
     *
     * The Image::format() function returns an item from the FormatType enumeration.
     * @since 2.2.0
     */
    public enum FormatType {
      INFRARED = 0,
      IBRG = 1
    }

    public enum ImageType {
      DEFAULT,
      RAW
    }

    public enum CameraType {
      LEFT = 0, //!< Left stereo camera
      RIGHT = 1 //!< Right stereo camera
    };
  }

}
