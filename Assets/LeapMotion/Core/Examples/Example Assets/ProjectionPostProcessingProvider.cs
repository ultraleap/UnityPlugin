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

  public class ProjectionPostProcessingProvider : PostProcessProvider {

    [Range(0f, 5f)]
    public float extensionAmount = 3.5f;

    [Range(0f, 1f)]
    public float handMergeDistance = 0.65f;

    public override void ProcessFrame(ref Frame inputFrame) {
      // Calculate the position of the head and the basis to calculate shoulder position.
      Vector3 headPos = Camera.main.transform.position;
      Quaternion shoulderBasis = Quaternion.LookRotation(
        Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up),
        Vector3.up);

      foreach (var hand in inputFrame.Hands) {
        // Approximate shoulder position with magic values.
        Vector3 shoulderPos = headPos
                              + (shoulderBasis * (new Vector3(0f, -0.2f, -0.1f)
                              + Vector3.left * 0.1f * (hand.IsLeft ? 1f : -1f)));

        // Calculate the projection amount and projection the position of the hand.
        float projectionAmount = Mathf.Max(1f,
          Mathf.Pow(Vector3.Distance(hand.PalmPosition.ToVector3(), shoulderPos)
                      + handMergeDistance,
                    extensionAmount));
        hand.SetTransform(
          ((hand.PalmPosition.ToVector3() - shoulderPos) * projectionAmount) + shoulderPos,
          hand.Rotation.ToQuaternion());
      }
    }
  }
}
