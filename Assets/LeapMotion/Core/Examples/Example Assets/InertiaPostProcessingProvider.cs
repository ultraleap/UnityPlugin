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
    
    [Tooltip("Higher stiffness will keep the bouncy hand closer to the tracked hand data.")]
    [Range(0f, 10f)]
    public float stiffness = 2f;
    
    [Tooltip("Higher damping will suppress more motion and reduce oscillation.")]
    [Range(0f, 10f)]
    public float damping = 2f;
    
    private Pose? _leftPose = null;
    private Pose? _previousLeftPose = null;
    private float _leftAge = 0f;

    private Pose? _rightPose = null;
    private Pose? _previousRightPose = null;
    private float _rightAge = 0f;

    /// <summary>
    /// Post-processes the input frame in place to give hands bouncy-feeling physics.
    /// </summary>
    public override void ProcessFrame(ref Frame inputFrame) {
      var leftHand = inputFrame.Hands.Query().FirstOrDefault(h => h.IsLeft);
      var rightHand = inputFrame.Hands.Query().FirstOrDefault(h => !h.IsLeft);

      processHand(leftHand, ref _leftPose, ref _previousLeftPose, ref _leftAge);
      processHand(rightHand, ref _rightPose, ref _previousRightPose, ref _rightAge);
    }
    
    private void processHand(Hand hand,
                             ref Pose? maybeCurPose,
                             ref Pose? maybePrevPose,
                             ref float handAge) {
      if (hand == null) {
        // Clear state.
        maybeCurPose = null;
        maybePrevPose = null;
        handAge = 0f;
      }
      else {
        var framePose = hand.GetPalmPose();

        if (!maybeCurPose.HasValue) {
          // The hand just started being tracked.
          maybePrevPose = null;
          maybeCurPose = framePose;
        }
        else if (!maybePrevPose.HasValue) {
          // Have current pose, lack previous pose, just get initial momentum.
          maybePrevPose = maybeCurPose;
          maybeCurPose = framePose;
        }
        else {
          // There's enough data to verlet-integrate.

          // Calculate how much time has passed since we last received hand data.
          // 
          // We can't actually assume leftHand.TimeVisible is monotonically increasing,
          // because there are independent hand data streams to account for the timing
          // differences of Update() and FixedUpdate()!
          // 
          // This is addressed by using the data mode setting so that one stream or the
          // other is selected, but some stateless post-processes may want to always
          // run every frame no matter what, so as a safety measure, we ensure deltaTime
          // is positive before running our stateful filter to give the hand momentum.
          var deltaTime = hand.TimeVisible - handAge;
          if (deltaTime > 0) {
            handAge = hand.TimeVisible;

            var curPose = maybeCurPose.Value;
            var prevPose = maybePrevPose.Value;
            integratePose(ref curPose, ref prevPose,
                          targetPose: framePose, deltaTime: deltaTime);
          }
        }
      }
    }

    /// <summary>
    /// Integrates curPose's inertia from prevPose to give it bouncy-feeling physics
    /// while gradually shifting it towards the target pose.
    /// </summary>
    private void integratePose(ref Pose curPose, ref Pose prevPose,
                               Pose targetPose, float deltaTime) {
      // Calculate motion from prevPose to curPose.
      var deltaPose = curPose.inverse * prevPose; // prevPose in curPose's local space.
      deltaPose = new Pose(-deltaPose.position, Quaternion.Inverse(deltaPose.rotation));
      deltaPose = Pose.Lerp(deltaPose, Pose.identity, damping * deltaTime); // Dampen.

      // Verlet-integrate curPose based on the delta from prevPose.
      Pose tempPose = curPose;
      curPose = curPose * deltaPose;
      prevPose = tempPose;

      // Pull the integrated hand toward the target a little bit based on stiffness.
      curPose = Pose.Lerp(curPose, targetPose, stiffness * deltaTime);
    }
  }
}
