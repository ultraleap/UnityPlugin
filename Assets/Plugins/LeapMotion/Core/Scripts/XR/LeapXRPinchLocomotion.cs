/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity {

  /// <summary>Ensure this script is on your player object and 
  /// set to execute before the LeapXRServiceProvider</summary>
  public class LeapXRPinchLocomotion : MonoBehaviour {

    [Tooltip("Your Leap Hand Provider.  Ensure the Pinch Locomotion script " +
      "is set to execute before this provider in the 'Script Execution Order'")]
    public LeapXRServiceProvider provider;
    [Range(0.00f, 50f)]
    public float pinchThreshold    = 25f;
    [Range(0.0f, 0.2f)]
    public float momentum          = 0.125f;
    public bool horizontalRotation = true;
    public bool enableScaling      = true;

    bool isLeftPinching, isRightPinching;
    Vector3 rootA = Vector3.zero, rootB = Vector3.one; // The stationary world-space anchors
    Vector3 curA  = Vector3.zero, curB  = Vector3.one; // The dynamic    world-space pinch points

    void Update() {
      Hand left          = HandUtils.Get(provider.CurrentFrame, Chirality.Left);
      Hand right         = HandUtils.Get(provider.CurrentFrame, Chirality.Right);
      bool leftPinching  = left  != null && left.PinchDistance  < pinchThreshold;
      bool rightPinching = right != null && right.PinchDistance < pinchThreshold;

      if (leftPinching && rightPinching) {             // Set Points when Both Pinched
        curA = left .GetPinchPosition();
        curB = right.GetPinchPosition();

        if (!isLeftPinching || !isRightPinching) {
          rootA = curA;
          rootB = curB;
        }
      } else if (leftPinching) {                       // Set Points when Left Pinched
        oneHandedPinchMove(left, isLeftPinching, isRightPinching,
                           ref rootA, ref curA, rootB, ref curB);
      } else if (rightPinching) {                      // Set Points when Right Pinched
        oneHandedPinchMove(right, isRightPinching, isLeftPinching,
                           ref rootB, ref curB, rootA, ref curA);
      } else {                                         // Apply Momentum to Dynamic Points when Unpinched
        curA = Vector3.Lerp(curA, rootA, momentum);
        curB = Vector3.Lerp(curB, rootB, momentum);
      }
      isLeftPinching  = leftPinching;
      isRightPinching = rightPinching;

      // Transform the root so the (dynamic) cur points match the (stationary) root points
      Vector3    pivot       = ((rootA + rootB) / 2);
      Vector3    translation = pivot - ((curA + curB) / 2);
      Quaternion rotation    = Quaternion.FromToRotation(Vector3.Scale(new Vector3(1f, horizontalRotation ? 0 : 1f, 1f), curB - curA),
                                                         Vector3.Scale(new Vector3(1f, horizontalRotation ? 0 : 1f, 1f), rootB - rootA));
      float scale = (rootA - rootB).magnitude / (curA - curB).magnitude;

      // Apply Translation
      transform.root.position += translation;

      if (rootA != rootB) {
        // Apply Rotation
        Pose curTrans           = new Pose(transform.root.position, transform.root.rotation);
             curTrans           = curTrans.Pivot(rotation, pivot);
        transform.root.position = curTrans.position; transform.root.rotation = curTrans.rotation;

        // Apply Scale about Pivot
        if (!float.IsNaN(scale) && enableScaling) {
          transform.root.position    = ((transform.root.position - pivot) * scale) + pivot;
          transform.root.localScale *= scale;
        }
      }

      provider.RetransformFrames();
    }

    /// <summary> Cheat Variable for one-handed momentum </summary>
    Vector3 residualMomentum = Vector3.zero;
    /// <summary> Ambidextrous function for handling one-handed pinch movement with momentum. </summary>
    void oneHandedPinchMove(Hand thisHand, bool thisIsPinching, bool otherIsPinching,
      ref Vector3 thisRoot, ref Vector3 thisCur, Vector3 otherRoot, ref Vector3 otherCur) {
      thisCur = thisHand.GetPinchPosition();

      if (!thisIsPinching || otherIsPinching) {
        residualMomentum = otherCur - otherRoot;
        thisRoot = thisCur;
      } else {
        otherCur = (otherRoot + (thisCur - thisRoot)) + residualMomentum;
      }
      residualMomentum *= 1f - momentum;
    }

  }

}
