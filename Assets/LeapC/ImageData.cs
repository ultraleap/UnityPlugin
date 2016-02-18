/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
namespace LeapInternal
{
    using System;
    using System.Runtime.InteropServices;
    using Leap;

    public class ImageData : PooledObject
    {
        public bool isComplete = false;
        public byte[] pixelBuffer;
        private GCHandle _bufferHandle;
        private bool _isPinned = false;
        private object locker = new object();

        public UInt64 index;
        public Int64 frame_id;
        public Int64 timestamp;

        public eLeapImageType type;
        public eLeapImageFormat format;
        public UInt32 bpp;
        public UInt32 width;
        public UInt32 height;
        public float RayOffsetX;
        public float RayOffsetY;
        public float RayScaleX;
        public float RayScaleY;
        public int DistortionSize;
        public UInt64 DistortionMatrixKey;
        public DistortionData DistortionData;

        public ImageData(){}
        public ImageData(UInt64 bufferLength, UInt64 index){
            pixelBuffer = new byte[bufferLength];
            this.index = index;
        }

        public void CompleteImageData(eLeapImageType type,
                                      eLeapImageFormat format,
                                      UInt32 bpp, 
                                      UInt32 width, 
                                      UInt32 height,
                                      Int64 timestamp,
                                      Int64 frame_id,
                                      float x_offset,
                                      float y_offset,
                                      float x_scale,
                                      float y_scale,
                                      DistortionData distortionData,
                                      int distortion_size,
                                      UInt64 distortion_matrix_version){
            lock(locker){
                this.type = type;
                this.format = format;
                this.bpp = bpp;
                this.width = width;
                this.height = height;
                this.timestamp = timestamp;
                this.frame_id = frame_id;
                this.RayOffsetX = x_offset;
                this.RayOffsetY = y_offset;
                this.RayScaleX = x_scale;
                this.RayScaleY = y_offset;
                this.DistortionData = distortionData;
                this.DistortionSize = distortion_size;
                this.DistortionMatrixKey = distortion_matrix_version;
                isComplete = true;
            }
        }

        public override void CheckIn ()
        {
            base.CheckIn();
            this.unPinHandle();
            this.index = 0;
            this.isComplete = false;
        }
        public IntPtr getPinnedHandle(){
            if(pixelBuffer == null)
                return IntPtr.Zero;

            lock(locker){
                if(!_isPinned){
                    _bufferHandle = GCHandle.Alloc(pixelBuffer, GCHandleType.Pinned);
                    _isPinned = true;
                }
            }
            return _bufferHandle.AddrOfPinnedObject();
        }

        public void unPinHandle(){
            lock(locker){
                if(_isPinned){
                    _bufferHandle.Free();
                    _isPinned = false;
                }
            }
        }

//        public ImageData Copy(){
//            ImageData copy = new ImageData();
//            copy.pixelBuffer = new byte[pixelBuffer.Length];
//            copy.index = this.index;
//            copy.type = this.type; 
//            copy.format = this.format;
//            copy.bpp = this.bpp;
//            copy.width = this.width;
//            copy.height = this.height;
//            copy.timestamp = this.timestamp;
//            copy.frame_id = this.frame_id;
//            copy.RayOffsetX = this.RayOffsetX;
//            copy.RayOffsetY = this.RayOffsetY;
//            copy.RayScaleX = this.RayScaleX;
//            copy.RayScaleY = this.RayScaleY;
//            copy.DistortionData = this.DistortionData;
//            copy.DistortionMatrixKey = this.DistortionMatrixKey;
//            copy.isComplete = this.isComplete;
//
//            return copy;
//        }
    }



}
