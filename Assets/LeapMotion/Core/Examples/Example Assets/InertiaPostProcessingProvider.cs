/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
namespace Leap.Unity.Examples {
  public class InertiaPostProcessingProvider : PostProcessProvider {
    [Range(0f, 1f)]
    public float _stiffness = 0.01f;
    [Range(0f, 1f)]
    public float _damping = 0.05f;

    private Pose leftHand = Pose.identity,
                 rightHand = Pose.identity,
                 previousLeftHand = Pose.identity,
                 previousRightHand = Pose.identity;

    public override void ProcessFrame(ref Frame inputFrame) {
      //Record the current pose states of the left and right hands
      int leftHandId = -1, rightHandId = -1;
      Pose currentLeftHand = Pose.identity,
           currentRightHand = Pose.identity;
      foreach (Hand hand in inputFrame.Hands) {
        if (hand.IsLeft) {
          leftHandId = hand.Id;
          currentLeftHand = new Pose(hand.PalmPosition.ToVector3(),
                                     hand.Rotation.ToQuaternion());
          if (hand.TimeVisible == 0f) {
            leftHand = currentLeftHand;
            previousLeftHand = currentLeftHand;
          }
        } else {
          rightHandId = hand.Id;
          currentRightHand = new Pose(hand.PalmPosition.ToVector3(),
                                      hand.Rotation.ToQuaternion());
          if (hand.TimeVisible == 0f) {
            rightHand = currentRightHand;
            previousRightHand = currentRightHand;
          }
        }
      }

      //Integrate the hands forward in time and apply their transforms to the frame
      if (leftHandId != -1) {
        integrateHand(ref leftHand, ref previousLeftHand, currentLeftHand);
        inputFrame.Hand(leftHandId).SetTransform(leftHand.position, leftHand.rotation);
      }
      if (rightHandId != -1) {
        integrateHand(ref rightHand, ref previousRightHand, currentRightHand);
        inputFrame.Hand(rightHandId).SetTransform(rightHand.position, rightHand.rotation);
      }
    }

    //Integrate's the hand's inertia to give it bouncy feeling physics
    void integrateHand(ref Pose hand, ref Pose previousHand, Pose currentHand) {
      //Verlet Integration
      Pose tempHand = hand;
      hand *= Pose.Lerp(previousHand.inverse * hand, Pose.identity, _damping); //5% damping/frame
      previousHand = tempHand;
      //Pull the integrated hand toward the original one a little bit every frame
      hand = Pose.Lerp(hand, currentHand, _stiffness);
    }
  }
}
