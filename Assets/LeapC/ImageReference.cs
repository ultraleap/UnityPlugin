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
    using Leap;

    public class ImageReference
    {
        public Image imageObject{get; set;}
        public ImageData imageData{get; set;}
        public long Timestamp{get; set;}

        public ImageReference(Image image, ImageData data, long timestamp){
            this.imageObject = image;
            this.imageData = data;
            this.Timestamp = timestamp;
        }
    }
}
