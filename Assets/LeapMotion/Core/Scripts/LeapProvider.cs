/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System;
using System.Collections;

namespace Leap.Unity {

  /// <summary>
  /// Provides Frame object data to the Unity application by firing events as soon
  /// as Frame data is available. Frames contain all currently tracked Hands in view
  /// of the Leap Motion Controller.
  /// </summary>
  public abstract class LeapProvider : MonoBehaviour {

    public TestHandFactory.TestHandPose editTimePose = TestHandFactory.TestHandPose.PoseA;

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

  public static class LeapProviderExtensions {

    public static Leap.Hand MakeTestHand(this LeapProvider provider, bool isLeft) {
      return TestHandFactory.MakeTestHand(isLeft, Hands.Provider.editTimePose)
                            .Transform(UnityMatrixExtension.GetLeapMatrix(Hands.Provider.transform));
    }

  }
}
