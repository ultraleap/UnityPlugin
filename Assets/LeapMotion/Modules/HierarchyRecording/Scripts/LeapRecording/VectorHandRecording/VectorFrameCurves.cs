/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Query;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Recording {

  /// <summary> VectorHand Animation Curve data struct for a Leap frame. </summary>
  [System.Serializable]
  public struct VectorFrameCurves {

    /// <summary> Hand curve data for the left hand. </summary>
    public VectorHandCurves leftHandCurves;

    /// <summary> Hand curve data for the right hand. </summary>
    public VectorHandCurves rightHandCurves;

    /// <summary>
    /// Adds keyframe data from the provided frame to this animation data at the time
    /// specified in seconds.
    /// 
    /// Data is only input from the first left and first right hands. If the frame lacks
    /// a left or a right hand, that hand is considered untracked.
    /// </summary>
    public void AddKeyframes(float keyframeTime, Frame frame) {
      var leftHand  = frame.Hands.Query().FirstOrDefault(h => h.IsLeft);
      var rightHand = frame.Hands.Query().FirstOrDefault(h => !h.IsLeft);

      leftHandCurves .AddKeyframes(keyframeTime, leftHand);
      rightHandCurves.AddKeyframes(keyframeTime, rightHand);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Processes all of the current keyframe data and compresses them using
    /// AnimationCurveUtil.
    /// </summary>
    public void CompressCurves() {
      leftHandCurves .Compress();
      rightHandCurves.Compress();
    }
#endif

    /// <summary>
    /// Samples the data in these VectorFrameCurves at the specified time into the
    /// provided frame.
    /// </summary>
    public void Sample(float time, Frame intoFrame, Hand intoLeftHand, Hand intoRightHand) {
      bool leftHandTracked  = leftHandCurves .Sample(time, intoLeftHand,  true);
      bool rightHandTracked = rightHandCurves.Sample(time, intoRightHand, false);

      intoFrame.Hands.Clear();
      if (leftHandTracked)  intoFrame.Hands.Add(intoLeftHand);
      if (rightHandTracked) intoFrame.Hands.Add(intoRightHand);

      intoFrame.Timestamp = (long)(time * LeapRecording.S_TO_NS);
    }

  }

}
