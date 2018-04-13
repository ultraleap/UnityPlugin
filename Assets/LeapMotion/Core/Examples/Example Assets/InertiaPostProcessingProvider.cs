/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Query;
using UnityEngine;

namespace Leap.Unity.Examples {

  public class InertiaPostProcessingProvider : PostProcessProvider {

    [Header("Inertia Settings")]
    
    [Range(0f, 1f)]
    public float stiffness = 0.01f;

    [Range(0f, 1f)]
    public float damping = 0.05f;

    private Pose _leftPose = Pose.identity;
    private Pose _rightPose = Pose.identity;
    private Pose _previousLeftPose = Pose.identity;
    private Pose _previousRightPose = Pose.identity;

    /// <summary>
    /// Post-processes the input frame in place to give hands bouncy-feeling physics.
    /// </summary>
    public override void ProcessFrame(ref Frame inputFrame) {

      var leftHand = inputFrame.Hands.Query().FirstOrDefault();
      var rightHand = inputFrame.Hands.Query().FirstOrDefault();

      if (leftHand != null) {
        var frameLeftPose = leftHand.GetPalmPose();

        if (leftHand.TimeVisible == 0) {
          // Initialize with no momentum.
          _leftPose = frameLeftPose;
          _previousLeftPose = frameLeftPose;
        }
        
        // Integrate hand pose with momentum, targeting the current frame pose.
        integratePose(ref _leftPose, ref _previousLeftPose,
                      targetPose: frameLeftPose);
      }
      if (rightHand != null) {
        var frameRightPose = rightHand.GetPalmPose();

        if (rightHand.TimeVisible == 0) {
          // Initialize with no momentum.
          _rightPose = frameRightPose;
          _previousRightPose = frameRightPose;
        }

        // Integrate hand pose with momentum, targeting the current frame pose.
        integratePose(ref _rightPose, ref _previousRightPose,
                      targetPose: frameRightPose);
      }
    }

    /// <summary>
    /// Integrates curPose's inertia from prevPose to give it bouncy-feeling physics
    /// while gradually shifting it towards the target pose.
    /// </summary>
    private void integratePose(ref Pose curPose, ref Pose prevPose, Pose targetPose) {
      // Verlet integration onto curPose based on the delta from prevPose.
      Pose tempPose = curPose;
      curPose *= Pose.Lerp(prevPose.inverse * curPose, Pose.identity, damping);
      prevPose = tempPose;

      // Pull the integrated hand toward the original one a little bit every frame.
      curPose = Pose.Lerp(curPose, targetPose, stiffness);
    }
  }
}
