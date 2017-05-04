/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections.Generic;
using InteractionEngineUtility;

namespace Leap.Unity.Interaction {

  /**
  * The ThrowingControllerSlidingWindow class averages the object's velocity
  * over the specified window and applies it to the object when released.
  * @since 4.1.4
  */
  public class ThrowingControllerSlidingWindow : IThrowingController {

    /** The length of the averaging window in seconds. */
    [Tooltip("How long the averaging window is in seconds.")]
    [SerializeField]
    private float _windowLength = 0.05f;

    /** The delay between the averaging window and the current time. */
    [Tooltip("The delay between the averaging window and the current time.")]
    [SerializeField]
    private float _windowDelay = 0.02f;

    /** 
     * Modifies the release velocity.
     *
     * Use this curve to modify the velocity transfered based on its canonical speed.
     * If the animation curve value is below 1.0 at a particular speed, then the transfered 
     * velocity is diminished; if the curve value is greater than one, the transfered 
     * velocity is amplified.
     * @since 4.1.4
     */
    [Tooltip("X axis is the speed of the released object.  Y axis is the value to multiply the speed by.")]
    [SerializeField]
    private AnimationCurve _velocityMultiplierCurve = new AnimationCurve(new Keyframe(0, 1, 0, 0),
                                                                    new Keyframe(3, 1.5f, 0, 0));

    private struct VelocitySample {
      public float time;
      public Vector3 position;
      public Quaternion rotation;

      public VelocitySample(Vector3 position, Quaternion rotation, float time) {
        this.position = position;
        this.rotation = rotation;
        this.time = Time.fixedTime;
      }

      public static VelocitySample Interpolate(VelocitySample a, VelocitySample b, float time) {
        float alpha = Mathf.Clamp01(Mathf.InverseLerp(a.time, b.time, time));
        return new VelocitySample(Vector3.Lerp(a.position, b.position, alpha),
                                  Quaternion.Slerp(a.rotation, b.rotation, alpha),
                                  time);
      }
    }

    private Queue<VelocitySample> _velocityQueue = new Queue<VelocitySample>(64);

    /** Samples the current velocity and adds it to the rolling average. */
    public override void OnHold(ReadonlyList<Hand> hands) {
      _velocityQueue.Enqueue(new VelocitySample(_obj.warper.RigidbodyPosition, _obj.warper.RigidbodyRotation, Time.fixedTime));

      while (true) {
        VelocitySample oldest = _velocityQueue.Peek();

        //Dequeue conservatively if the oldest is more than 4 frames later than the start of the window
        if (oldest.time + Time.fixedDeltaTime * 4 < Time.fixedTime - _windowLength - _windowDelay) {
          _velocityQueue.Dequeue();
        } else {
          break;
        }
      }
    }

    /** Transfers the averaged velocity to the released object. */
    public override void OnThrow(Hand throwingHand) {
      if (_velocityQueue.Count < 2) {
        _obj.rigidbody.velocity = Vector3.zero;
        _obj.rigidbody.angularVelocity = Vector3.zero;
        return;
      }

      float windowEnd = Time.fixedTime - _windowDelay;
      float windowStart = windowEnd - _windowLength;

      //0 occurs before 1
      //start occurs before end
      VelocitySample start0, start1;
      VelocitySample end0, end1;
      VelocitySample s0, s1;

      s0 = s1 = start0 = start1 = end0 = end1 = _velocityQueue.Dequeue();

      while (_velocityQueue.Count != 0) {
        s0 = s1;
        s1 = _velocityQueue.Dequeue();

        if (s0.time < windowStart && s1.time >= windowStart) {
          start0 = s0;
          start1 = s1;
        }

        if (s0.time < windowEnd && s1.time >= windowEnd) {
          end0 = s0;
          end1 = s1;

          //We have assigned both start and end and can break out of loop
          _velocityQueue.Clear();
          break;
        }
      }

      VelocitySample start = VelocitySample.Interpolate(start0, start1, windowStart);
      VelocitySample end = VelocitySample.Interpolate(end0, end1, windowEnd);

      Vector3 interpolatedVelocity = PhysicsUtility.ToLinearVelocity(start.position, end.position, _windowLength);

      //If trying to throw the object backwards into the hand
      Vector3 relativeVelocity = interpolatedVelocity - throwingHand.PalmVelocity.ToVector3();
      if (Vector3.Dot(relativeVelocity, throwingHand.PalmNormal.ToVector3()) < 0) {
        interpolatedVelocity -= Vector3.Project(relativeVelocity, throwingHand.PalmNormal.ToVector3());
      }

      _obj.rigidbody.velocity = interpolatedVelocity;
      _obj.rigidbody.angularVelocity = PhysicsUtility.ToAngularVelocity(start.rotation, end.rotation, _windowLength);

      _obj.rigidbody.velocity *= _velocityMultiplierCurve.Evaluate(_obj.rigidbody.velocity.magnitude);
    }
  }
}
