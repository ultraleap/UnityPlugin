/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
using System;
using System.Collections.Generic;
using Leap;

namespace LeapInternal
{
    //TODO test thread safety
    public class DistortionDictionary : Dictionary<UInt64, DistortionData>{

        private UInt64 _currentMatrix = 0;
        private bool _distortionChange = false;
        private object locker = new object();

        public UInt64 CurrentMatrix{
            get{
                lock(locker){
                    return _currentMatrix;
                }
            }
            set {
                lock(locker){
                    _currentMatrix = value;
                }
            }
        }
        public bool DistortionChange{
            get{
                lock(locker){
                    return _distortionChange;
                }
            }
            set {
                lock(locker){
                    _distortionChange = value;
                }
            }

        }

        public DistortionData GetMatrix(UInt64 version){
            lock(locker){
                DistortionData matrix;
                this.TryGetValue(version, out matrix);
                return matrix;
            }
        }

        public bool VersionExists(UInt64 version){
            lock(locker){
                return this.ContainsKey(version);
            }
        }

    }
}

