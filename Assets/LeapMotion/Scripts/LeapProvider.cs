using UnityEngine;
using System;
using System.Collections;

namespace Leap.Unity {
  /**LeapProvider's supply images and Leap Hands */
  public abstract class LeapProvider : MonoBehaviour {
    public event Action<Frame> OnUpdateFrame;
    public event Action<Frame> OnFixedFrame;

    public abstract Frame CurrentFrame { get; }
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
