/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace LeapInternal {
  using System;
  using Leap;

  public class ImageData {
    private LEAP_IMAGE_PROPERTIES _properties;
    private object _object;

    public Image.CameraType camera { get; protected set; }
    public eLeapImageType type { get { return _properties.type; } }
    public eLeapImageFormat format { get { return _properties.format; } }
    public UInt32 bpp { get { return _properties.bpp; } }
    public UInt32 width { get { return _properties.width; } }
    public UInt32 height { get { return _properties.height; } }
    public float RayScaleX { get { return _properties.x_scale; } }
    public float RayScaleY { get { return _properties.y_scale; } }
    public float RayOffsetX { get { return _properties.x_offset; } }
    public float RayOffsetY { get { return _properties.y_offset; } }
    public byte[] AsByteArray { get { return _object as byte[]; } }
    public float[] AsFloatArray { get { return _object as float[]; } }
    public UInt32 byteOffset { get; protected set; }

    public int DistortionSize { get { return LeapC.DistortionSize; } }
    public UInt64 DistortionMatrixKey { get; protected set; }
    public DistortionData DistortionData { get; protected set; }

    public ImageData(Image.CameraType camera, LEAP_IMAGE image, DistortionData distortionData) {
      this.camera = camera;
      this._properties = image.properties;
      this.DistortionMatrixKey = image.matrix_version;
      this.DistortionData = distortionData;
      this._object = MemoryManager.GetPinnedObject(image.data);
      this.byteOffset = image.offset;
    }
  }
}
