using InteractionEngineUtility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  /// <summary>
  /// The sliding window throw controller implementation offers a simple heuristic
  /// that provides a reasonable accurate measure of the user's intended
  /// "throw direction" for a physical object. It is used as the default
  /// implementation of an Interaction Behaviour's throw controller.
  /// </summary>
  public class SlidingWindowThrow : IThrowController {

    /// <summary> The length of the averaging window in seconds </summary>
    private float _windowLength = 0.05f;

    /// <summary> The delay between the averaging window and the current time. </summary>
    private float _windowDelay = 0.02f;

    /// <summary>
    /// A curve that maps the speed of the object upon release to a multiplier to apply
    /// to that speed as the throw occurs.
    /// </summary>
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

    /// <summary> Samples the current velocity and adds it to a rolling average. </summary>
    public void OnHold(InteractionBehaviour intObj, ReadonlyList<InteractionHand> hands) {
      _velocityQueue.Enqueue(new VelocitySample(
        intObj.rigidbody.position, intObj.rigidbody.rotation, Time.fixedTime));

      while (true) {
        VelocitySample oldest = _velocityQueue.Peek();

        // Dequeue conservatively if the oldest is more than 4 frames later
        // than the start of the window.
        if (oldest.time + Time.fixedDeltaTime * 4 < Time.fixedTime - _windowLength - _windowDelay) {
          _velocityQueue.Dequeue();
        }
        else {
          break;
        }
      }
    }

    /// <summary> Transfers the averaged velocity to the released object. </summary>
    public void OnThrow(InteractionBehaviour intObj, InteractionHand throwingHand) {
      if (_velocityQueue.Count < 2) {
        intObj.rigidbody.velocity = Vector3.zero;
        intObj.rigidbody.angularVelocity = Vector3.zero;
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
      Vector3 relativeVelocity = interpolatedVelocity - throwingHand.GetLastTrackedLeapHand().PalmVelocity.ToVector3();
      if (Vector3.Dot(relativeVelocity, throwingHand.GetLastTrackedLeapHand().PalmNormal.ToVector3()) < 0) {
        interpolatedVelocity -= Vector3.Project(relativeVelocity, throwingHand.GetLastTrackedLeapHand().PalmNormal.ToVector3());
      }

      intObj.rigidbody.velocity = interpolatedVelocity;
      intObj.rigidbody.angularVelocity = PhysicsUtility.ToAngularVelocity(start.rotation,
                                                                          end.rotation,
                                                                          _windowLength);

      intObj.rigidbody.velocity *= _velocityMultiplierCurve.Evaluate(intObj.rigidbody.velocity.magnitude);
    }

  }

}