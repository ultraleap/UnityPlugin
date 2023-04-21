/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Interaction.Internal.InteractionEngineUtility;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction
{
    /// <summary>
    /// The sliding window throw handler implements a simple heuristic that provides a
    /// reasonably accurate measure of the user's intended "throw direction" for a physical
    /// object. It is used as the default implementation of an Interaction Behaviour's
    /// throw handler.
    /// </summary>
    public class SlidingWindowThrow : IThrowHandler
    {

        /// <summary>
        /// The length of the averaging window in seconds.
        /// </summary>
        private float _windowLength = 0.05F;

        /// <summary>
        /// The delay between the averaging window and the current time.
        /// </summary>
        private float _windowDelay = 0.02F;

        /// <summary>
        /// A curve that maps the speed of the object upon release to a multiplier to apply
        /// to that speed as the throw occurs.
        /// </summary>
        private AnimationCurve _velocityMultiplierCurve = new AnimationCurve(
                                                            new Keyframe(0.0F, 1.0F, 0, 0),
                                                            new Keyframe(3.0F, 1.5F, 0, 0));

        private struct VelocitySample
        {
            public float time;
            public Vector3 position;
            public Quaternion rotation;

            public VelocitySample(Vector3 position, Quaternion rotation, float time)
            {
                this.position = position;
                this.rotation = rotation;
                this.time = Time.fixedTime;
            }

            public static VelocitySample Interpolate(VelocitySample a, VelocitySample b, float time)
            {
                float alpha = Mathf.Clamp01(Mathf.InverseLerp(a.time, b.time, time));

                return new VelocitySample(Vector3.Lerp(a.position, b.position, alpha),
                                          Quaternion.Slerp(a.rotation, b.rotation, alpha),
                                          time);
            }
        }

        private Queue<VelocitySample> _velocityQueue = new Queue<VelocitySample>(64);

        /// <summary>
        /// Samples the current velocity and adds it to a rolling average.
        /// </summary>
        public void OnHold(InteractionBehaviour intObj,
                           IReadOnlyList<InteractionController> controllers)
        {
            _velocityQueue.Enqueue(new VelocitySample(intObj.rigidbody.position,
                                                      intObj.rigidbody.rotation,
                                                      Time.fixedTime));

            while (true)
            {
                VelocitySample oldestVelocity = _velocityQueue.Peek();

                // Dequeue conservatively if the oldest velocity is more than 4 frames later
                // than the start of the window.
                if (oldestVelocity.time + (Time.fixedDeltaTime * 4) < Time.fixedTime
                                                                      - _windowLength
                                                                      - _windowDelay)
                {
                    _velocityQueue.Dequeue();
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Transfers the averaged velocity to the released object.
        /// </summary>
        public void OnThrow(InteractionBehaviour intObj, InteractionController throwingController)
        {
            if (_velocityQueue.Count < 2)
            {
                intObj.rigidbody.velocity = Vector3.zero;
                intObj.rigidbody.angularVelocity = Vector3.zero;
                return;
            }

            float windowEnd = Time.fixedTime - _windowDelay;
            float windowStart = windowEnd - _windowLength;

            // 0 occurs before 1,
            // start occurs before end.
            VelocitySample start0, start1;
            VelocitySample end0, end1;
            VelocitySample s0, s1;

            s0 = s1 = start0 = start1 = end0 = end1 = _velocityQueue.Dequeue();

            while (_velocityQueue.Count != 0)
            {
                s0 = s1;
                s1 = _velocityQueue.Dequeue();

                if (s0.time < windowStart && s1.time >= windowStart)
                {
                    start0 = s0;
                    start1 = s1;
                }

                if (s0.time < windowEnd && s1.time >= windowEnd)
                {
                    end0 = s0;
                    end1 = s1;

                    // We have assigned both start and end and can break out of the loop.
                    _velocityQueue.Clear();
                    break;
                }
            }

            VelocitySample start = VelocitySample.Interpolate(start0, start1, windowStart);
            VelocitySample end = VelocitySample.Interpolate(end0, end1, windowEnd);

            Vector3 interpolatedVelocity = PhysicsUtility.ToLinearVelocity(start.position,
                                                                           end.position,
                                                                           _windowLength);

            intObj.rigidbody.velocity = interpolatedVelocity;
            intObj.rigidbody.angularVelocity = PhysicsUtility.ToAngularVelocity(start.rotation,
                                                                                end.rotation,
                                                                                _windowLength);

            intObj.rigidbody.velocity *= _velocityMultiplierCurve.Evaluate(intObj.rigidbody.velocity.magnitude);
        }
    }
}