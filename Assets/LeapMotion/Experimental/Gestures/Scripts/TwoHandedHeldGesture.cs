using System.Collections;
using System.Collections.Generic;
using Leap.Unity.RuntimeGizmos;
using UnityEngine;

namespace Leap.Unity.Gestures {

  public abstract class TwoHandedHeldGesture : TwoHandedGesture, IPoseGesture {

    #region Constants

    /// <summary>
    /// Convenience for children to have a common distance for determining when
    /// two points (e.g. fingertips) are "touching". Use _SQR to check squared distances.
    /// </summary>
    protected const float MAX_TOUCHING_DISTANCE = 0.03f;
    protected const float MAX_TOUCHING_DISTANCE_SQR = MAX_TOUCHING_DISTANCE
                                                       * MAX_TOUCHING_DISTANCE;

    private const float MAX_HAND_BODY_VELOCITY = 2f; // m/s
    private const float MAX_RELATIVE_HAND_VELOCITY_SQR = MAX_HAND_BODY_VELOCITY
                                                     * MAX_HAND_BODY_VELOCITY;

    /// <summary>
    /// Convenience for children to have a common angle for determinine when two vectors
    /// (e.g. finger directions) are aligned.
    /// </summary>
    protected const float MAX_ALIGNED_ANGLE = 40f;

    #endregion

    #region Private Memory

    private DeltaBuffer leftMinusRightHandPosBuffer = new DeltaBuffer(5);

    /// <summary>
    /// Set to true when the gesture finishes; this gesture won't be able to activate
    /// again unless the user moves their hands into a configuration that isn't the
    /// held gesture pose.
    /// </summary>
    private bool _gestureReady = true;

    private float _gestureActiveTime = 0f;

    private Vector3 _lastKnownPositionOfInterest = Vector3.zero;

    #endregion

    #region Inspector

    [Header("Held Gesture")]
    public float holdActivationDuration = 0.25f;

    public bool drawHeldPoseDebug  = false;

    #endregion

    #region Abstract

    public abstract bool IsGesturePoseHeld(Hand leftHand, Hand rightHand,
                                           out Vector3 positionOfInterest);

    #endregion

    #region IPoseGesture

    public Pose pose {
      get {
        return new Pose(_lastKnownPositionOfInterest, Quaternion.identity);
      }
    }

    #endregion

    #region TwoHandedGesture Implementation

    protected override bool ShouldGestureActivate(Hand leftHand, Hand rightHand) {
      Vector3 positionOfInterest;
      bool isGesturePoseHeld = IsGesturePoseHeld(leftHand, rightHand, out positionOfInterest);
      _lastKnownPositionOfInterest = positionOfInterest;

      if (!isGesturePoseHeld && !_gestureReady) {
        _gestureReady = true;
      }

      bool isRelativeHandVelocityLow = updateIsRelativeHandVelocityLow(leftHand,
                                                                       rightHand);

      if (isRelativeHandVelocityLow && isGesturePoseHeld && _gestureReady) {

        if (drawHeldPoseDebug) {
          DebugPing.Ping(positionOfInterest, LeapColor.red, 0.10f);
        }

        return true;
      }

      return false;
    }

    private bool updateIsRelativeHandVelocityLow(Hand leftHand, Hand rightHand) {
      var leftHandPos = leftHand.PalmPosition.ToVector3();
      var rightHandPos = rightHand.PalmPosition.ToVector3();
      var leftMinusRightHandPosition = leftHandPos - rightHandPos;

      leftMinusRightHandPosBuffer.Add(leftMinusRightHandPosition, Time.time);

      if (leftMinusRightHandPosBuffer.IsFull) {
        var relativeHandVelocity = leftMinusRightHandPosBuffer.Delta();

        if (drawHeldPoseDebug) {
          RuntimeGizmoDrawer drawer = null;
          if (RuntimeGizmoManager.TryGetGizmoDrawer(out drawer)) {
            drawer.DrawBar(relativeHandVelocity.magnitude,
                           (leftHandPos + rightHandPos) / 2f,
                           Vector3.up, 0.1f);
          }
        }

        if (relativeHandVelocity.sqrMagnitude < MAX_RELATIVE_HAND_VELOCITY_SQR) {
          return true;
        }
        else {
          if (drawHeldPoseDebug) {
            DebugPing.Ping((leftHandPos + rightHandPos) / 2f, LeapColor.black, 0.2f);
          }
          return false;
        }
      }

      if (drawHeldPoseDebug) {
        DebugPing.Ping((leftHandPos + rightHandPos) / 2f, LeapColor.gray, 0.1f);
      }

      return false;
    }

    protected override bool ShouldGestureDeactivate(Hand leftHand, Hand rightHand,
        out DeactivationReason? deactivationReason) {

      Vector3 positionOfInterest;
      bool isGesturePoseHeld = IsGesturePoseHeld(leftHand, rightHand, out positionOfInterest);
      _lastKnownPositionOfInterest = positionOfInterest;

      if (updateIsRelativeHandVelocityLow(leftHand, rightHand)
          && isGesturePoseHeld) {
        if (_gestureActiveTime > holdActivationDuration) {
          // Gesture finished successfully.
          if (drawHeldPoseDebug) {
            DebugPing.Ping(positionOfInterest, LeapColor.cerulean, 0.20f);
          }

          deactivationReason = DeactivationReason.FinishedGesture;
          _gestureReady = false;

          return true;
        }
        else {
          // Continue gesture activation.
          deactivationReason = null;
          return false;
        }
      }
      else {
        // Gesture was cancelled.
        deactivationReason = DeactivationReason.CancelledGesture;
        return true;
      }

    }

    protected override void WhileHandUntracked(bool wasLeftHand) {
      base.WhileHandUntracked(wasLeftHand);

      leftMinusRightHandPosBuffer.Clear();
    }

    protected override void WhenGestureDeactivated(Hand maybeNullLeftHand,
                                                   Hand maybeNullRightHand,
                                                   DeactivationReason reason) {
      base.WhenGestureDeactivated(maybeNullLeftHand, maybeNullRightHand, reason);

      leftMinusRightHandPosBuffer.Clear();
    }

    protected override void WhileGestureActive(Hand leftHand, Hand rightHand) {
      base.WhileGestureActive(leftHand, rightHand);

      _gestureActiveTime += Time.deltaTime;
    }

    protected override void WhileGestureInactive(Hand maybeNullLeftHand, Hand maybeNullRightHand) {
      base.WhileGestureInactive(maybeNullLeftHand, maybeNullRightHand);

      _gestureActiveTime = 0f;
    }

    #endregion

  }

}