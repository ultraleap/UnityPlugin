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
  public class ProjectionProvider : PostProcessProvider {
    [Range(0f, 5f)]
    public float _extensionAmount = 3.5f;
    [Range(0f, 1f)]
    public float _handMergeDistance = 0.65f;
    public override void ProcessFrame(ref Frame inputFrame) {
      //Calculate the position of the head and the rotation of the shoulders
      Vector3 headPos = Camera.main.transform.position;
      Quaternion shoulderRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up));
      foreach (Hand hand in inputFrame.Hands) {
        //Approximate shoulder positions with magic values
        Vector3 shoulderPos = headPos + (shoulderRotation * (new Vector3(0f, -0.2f, -0.1f) + Vector3.left * 0.1f * (hand.IsLeft ? 1f : -1f)));
        //Calculate the scaling amount and scale the motion of the hands
        float scalingAmount = Mathf.Max(1f, Mathf.Pow(Vector3.Distance(hand.PalmPosition.ToVector3(), shoulderPos) + _handMergeDistance, _extensionAmount));
        hand.SetTransform(((hand.PalmPosition.ToVector3() - shoulderPos) * scalingAmount) + shoulderPos, hand.Rotation.ToQuaternion());
      }
    }
  }
}
