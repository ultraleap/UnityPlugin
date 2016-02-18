/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
using System;
using Leap;

namespace LeapInternal
{
    /** 
     * A CircularObjectBuffer specialized for image retrieval.
     */
    public class CircularImageBuffer: CircularObjectBuffer<Image>
    {
        public CircularImageBuffer(int capacity):base(capacity){}

        private Image _latestIR;
        private Image _latestRaw;

        public override void Put (Image item)
        {
            base.Put (item);
            if(item.Type == Image.ImageType.DEFAULT){
                _latestIR = item;
//                System.IO.File.WriteAllBytes(("default" + item.SequenceId + ".raw"), item.Data);
            } else if (item.Type == Image.ImageType.RAW){
                _latestRaw = item;
//                System.IO.File.WriteAllBytes(("raw" + item.SequenceId + ".raw"), item.Data);
            }
        }

//        public ImageList GetLatestImages(){
//            ImageList latest = new ImageList();
//            latest.IRLeft = _latestIRLeft;
//            latest.IRRight = _latestIRRight;
//            latest.RawLeft = _latestRawLeft;
//            latest.RawRight = _latestRawRight;
//            return latest;
//        }
//
//        public void GetLatestImages(ImageList receiver){
//            if(receiver == null)
//                receiver = new ImageList();
//            receiver.IRLeft = _latestIRLeft;
//            receiver.IRRight = _latestIRRight;
//            receiver.RawLeft = _latestRawLeft;
//            receiver.RawRight = _latestRawRight;
//        }

        public void GetLatestImages(out Image ir, out Image raw){
            ir = _latestIR;
            raw = _latestRaw;
        }

//        public int GetImagesForFrame(long frameId, ImageList receiver){
//            if( receiver == null)
//                receiver = new ImageList();
//            int foundCount = 0;
//            for(int i = 0; i < this.Count; i ++){
//                Image image = this.Get (i);
//                if(image.SequenceId == frameId){ 
//                    receiver.Add (image);
//                    foundCount++;
//                } else if(image.SequenceId < frameId) break;
//            }
//            return foundCount;
//        }
    }
}

