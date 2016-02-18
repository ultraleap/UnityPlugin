/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
using System;

namespace LeapInternal{

    public class PooledObject{
        public UInt64 poolIndex;
        public UInt64 age = 0;

        public virtual void CheckIn(){
            age = 0;
            poolIndex = 0;
        }
    }
}