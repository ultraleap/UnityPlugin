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
  public class PendingImages
  {
    List<ImageFuture> _pending = new List<ImageFuture>(20);
    UInt32 _pendingTimeLimit = 90000; // microseconds
    private object _locker = new object();

    public UInt32 pendingTimeLimit { get{ return _pendingTimeLimit; } 
                                     set{ _pendingTimeLimit = value; } }

    public void Add(ImageFuture pendingImage)
    {
      lock(_locker){
        _pending.Add(pendingImage);
      }
    }

    public ImageFuture FindAndRemove(LEAP_IMAGE_FRAME_REQUEST_TOKEN token)
    {
      lock(_locker){
        for (int i = 0; i < _pending.Count; i++) {
          ImageFuture ir = _pending[i];
          if (ir.Token.requestID == token.requestID) {
            _pending.RemoveAt(i);
            return ir;
          }
        }
      } 
      return null;
    }
        
    public int purgeOld(IntPtr connection)
    {
      Int64 now = LeapC.GetNow();
      int purgeCount = 0;
      lock(_locker){
        for (int i = _pending.Count - 1; i >= 0; i--) {
          ImageFuture ir = _pending[i];
          if ((now - ir.Timestamp) > pendingTimeLimit){
            _pending.RemoveAt(i);
            LeapC.CancelImageFrameRequest(connection, ir.Token);
            purgeCount++;
          }
        }
      }
      return purgeCount;
    }

    public int purgeAll(IntPtr connection)
    {
      int purgeCount = 0;
      lock(_locker){
        purgeCount = _pending.Count;
        for (int i = 0; i < _pending.Count; i++) {
          LeapC.CancelImageFrameRequest(connection, _pending[i].Token);
        }
        _pending.Clear();
      }
      return purgeCount;
    }

  }
}
