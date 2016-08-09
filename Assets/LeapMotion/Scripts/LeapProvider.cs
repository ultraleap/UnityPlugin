using UnityEngine;
using System;
using System.Collections;

namespace Leap.Unity {
  /**LeapProvider's supply images and Leap Hands */
  public abstract class LeapProvider : MonoBehaviour {
    public event Action<Frame> OnUpdateFrame;
    public event Action<Frame> OnFixedFrame;

    /// <summary>
    /// The current frame for this update cycle, in world space. 
    /// 
    /// IMPORTANT!  This frame might be mutable!  If you hold onto a reference
    /// to this frame, or a reference to any object that is a part of this frame,
    /// it might change unexpectedly.  If you want to save a reference, make sure
    /// to make a copy.
    /// </summary>
    public abstract Frame CurrentFrame { get; }

    /// <summary>
    /// The current frame for this fixed update cycle, in world space.
    /// 
    /// IMPORTANT!  This frame might be mutable!  If you hold onto a reference
    /// to this frame, or a reference to any object that is a part of this frame,
    /// it might change unexpectedly.  If you want to save a reference, make sure
    /// to make a copy.
    /// </summary>
    public abstract Frame CurrentFixedFrame { get; }

    public abstract Image CurrentImage { get; }

    protected void DispatchUpdateFrameEvent(Frame frame) {
      if (OnUpdateFrame != null) {
        OnUpdateFrame(frame);
      }
    }

    protected void DispatchFixedFrameEvent(Frame frame) {
      if (OnFixedFrame != null) {
        OnFixedFrame(frame);
      }
    }
  }
}
