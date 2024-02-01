/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity;

using System.Linq;
using UnityEngine;

namespace Leap.Examples
{

    public class InertiaPostProcessProvider : PostProcessProvider
    {

        [Header("Inertia")]

        [Tooltip("Higher stiffness will keep the bouncy hand closer to the tracked hand data.")]
        [Range(0f, 10f)]
        public float stiffness = 2f;

        [Tooltip("Higher damping will suppress more motion and reduce oscillation.")]
        [Range(0f, 10f)]
        public float damping = 2f;

        // Update-time Hand Data
        private Pose? _leftPose = null;
        private Pose? _previousLeftPose = null;
        private float _leftAge = 0f;
        private Pose? _rightPose = null;
        private Pose? _previousRightPose = null;
        private float _rightAge = 0f;

        // FixedUpdate-time Hand Data
        private Pose? _fixedLeftPose = null;
        private Pose? _fixedPreviousLeftPose = null;
        private float _fixedLeftAge = 0f;
        private Pose? _fixedRightPose = null;
        private Pose? _fixedPreviousRightPose = null;
        private float _fixedRightAge = 0f;

        /// <summary>
        /// Post-processes the input frame in place to give hands bouncy-feeling physics.
        /// </summary>
        public override void ProcessFrame(ref Frame inputFrame)
        {
            var leftHand = inputFrame.Hands.FirstOrDefault(h => h.IsLeft);
            var rightHand = inputFrame.Hands.FirstOrDefault(h => !h.IsLeft);

            // Frames can potentially come from two time-interwoven sources: Update frames
            // and FixedUpdate frames. Time is not monotonically increasing frame-to-frame
            // because FixedUpdates and Updates interweave and occasionally FixedUpdate plays
            // catch-up, and we interpolate hand data accordingly further up the hand pipeline,
            // which affects the hand.TimeVisible property we use to simulate our effect
            // statefully over time.
            //
            // To support both Update-time hand data and FixedUpdate-time hand data with a
            // single stateful post-process, we maintain two independent states for each stream,
            // which, independently, _are_ going to be monotonically forward-moving in time.
            if (Time.inFixedTimeStep)
            {
                // FixedUpdate hand data.
                processHand(leftHand,
                  ref _fixedLeftPose, ref _fixedPreviousLeftPose, ref _fixedLeftAge);
                processHand(rightHand,
                  ref _fixedRightPose, ref _fixedPreviousRightPose, ref _fixedRightAge);
            }
            else
            {
                // Update hand data.
                processHand(leftHand, ref _leftPose, ref _previousLeftPose, ref _leftAge);
                processHand(rightHand, ref _rightPose, ref _previousRightPose, ref _rightAge);
            }

        }

        private void processHand(Hand hand,
                                 ref Pose? maybeCurPose,
                                 ref Pose? maybePrevPose,
                                 ref float handAge)
        {
            if (hand == null)
            {
                // Clear state.
                maybeCurPose = null;
                maybePrevPose = null;
                handAge = 0f;
            }
            else
            {
                var framePose = hand.GetPalmPose();

                if (!maybeCurPose.HasValue)
                {
                    // The hand just started being tracked.
                    maybePrevPose = null;
                    maybeCurPose = framePose;
                }
                else if (!maybePrevPose.HasValue)
                {
                    // Have current pose, lack previous pose, just get initial momentum.
                    maybePrevPose = maybeCurPose;
                    maybeCurPose = framePose;
                }
                else
                {
                    // There's enough data to verlet-integrate.

                    // Calculate how much time has passed since we last received hand data.
                    //
                    // As a safety measure, we ensure deltaTime is positive before running our
                    // stateful filter to give the hand momentum. Any post-process could mess with
                    // the TimeVisible property, so we do this to minimize the chance of total
                    // havok.
                    var deltaTime = hand.TimeVisible - handAge;
                    if (deltaTime > 0)
                    {
                        handAge = hand.TimeVisible;

                        var curPose = maybeCurPose.Value;
                        var prevPose = maybePrevPose.Value;
                        integratePose(ref curPose, ref prevPose,
                                      targetPose: framePose, deltaTime: deltaTime);
                        hand.SetPalmPose(curPose);
                        maybeCurPose = curPose;
                        maybePrevPose = prevPose;
                    }
                }
            }
        }

        /// <summary>
        /// Integrates curPose's inertia from prevPose to give it bouncy-feeling physics
        /// while gradually shifting it towards the target pose.
        /// </summary>
        private void integratePose(ref Pose curPose, ref Pose prevPose,
                                   Pose targetPose, float deltaTime)
        {
            // Calculate motion from prevPose to curPose.
            var deltaPose = curPose.inverse().mul(prevPose); // prevPose in curPose's local space.
            deltaPose = new Pose(-deltaPose.position, Quaternion.Inverse(deltaPose.rotation));
            deltaPose = deltaPose.Lerp(Pose.identity, damping * deltaTime); // Dampen.

            // Verlet-integrate curPose based on the delta from prevPose.
            Pose tempPose = curPose;
            curPose = curPose.mul(deltaPose);
            prevPose = tempPose;

            // Pull the integrated hand toward the target a little bit based on stiffness.
            curPose = curPose.Lerp(targetPose, stiffness * deltaTime);
        }
    }
}