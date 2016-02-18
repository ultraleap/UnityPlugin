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
    public class DistortionData{
        public DistortionData(){}
        public DistortionData(UInt64 version, float width, float height, float[] data){
            Version = version;
            Width = width;
            Height = height;
            Data = data;
        }
        public UInt64 Version{get; set;}
        public float Width{get; set;}
        public float Height{get; set;}
        public float[] Data{get; set;}
        public bool IsValid{
            get{
                if(Data != null &&
                    Width == LeapInternal.LeapC.DistortionSize &&
                    Height == LeapInternal.LeapC.DistortionSize &&
                    Data.Length == Width * Height * 2 * 2)
                    return true;

                return false;
            }
        }
    }
}

