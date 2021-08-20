/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using System;

namespace Leap.Unity {

  using TestHandPose = TestHandFactory.TestHandPose;

  /// <summary>
  /// Provides Frame object data to the Unity application by firing events as soon
  /// as Frame data is available. Frames contain all currently tracked Hands in view
  /// of the Leap Motion Controller.
  /// </summary>
  public abstract class LeapProvider : MonoBehaviour {
    
    public TestHandPose editTimePose = TestHandPose.HeadMountedA;

    public event Action<Frame> OnUpdateFrame;
    public event Action<Frame> OnFixedFrame;
    public event Action<Frame> OnPostUpdateFrame;

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

    protected void DispatchUpdateFrameEvent(Frame frame) {
      if (OnUpdateFrame != null) {
        OnUpdateFrame(frame);
      }
      if (OnPostUpdateFrame != null) {
        OnPostUpdateFrame(frame);
      }
    }

    protected void DispatchFixedFrameEvent(Frame frame) {
      if (OnFixedFrame != null) {
        OnFixedFrame(frame);
      }
    }

  }

  public static class LeapProviderExtensions {

    public static Leap.Hand MakeTestHand(this LeapProvider provider, bool isLeft) {
      return TestHandFactory.MakeTestHand(isLeft, Hands.Provider.editTimePose)
                            .Transform(UnityMatrixExtension.GetLeapMatrix(Hands.Provider.transform));
    }

  }
}
