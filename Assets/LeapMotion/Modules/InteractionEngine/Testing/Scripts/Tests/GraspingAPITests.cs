/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

#if LEAP_TESTS
using Leap.Unity.Query;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Leap.Unity.Interaction.Tests {

  /// <summary>
  /// Tests for various parts of the IE API for grasping.
  /// </summary>
  public class GraspingAPITests : InteractionEngineTestBase {

    [UnityTest]
    public IEnumerator CanTryGraspObject() {
      yield return wait(beginningTestWait);

      InitTest(GRASP_THROW_RIG, DEFAULT_STAGE);

      // Move box0 out of position so it doesn't get grasped by the recording.
      box0.transform.position += Vector3.forward * 0.50f;
      box0.transform.localScale = box0.transform.localScale * 1.5f;

      yield return wait(aBit);

      // Play the grasp and throw recording. 
      recording.Play();
      yield return wait(50);

      recording.Pause();
      yield return wait(5);

      // Mid-grasp, teleport the box into a reasonable 'grasped' position.
      Vector3 fingertipAvg = rightHand.leapHand.Fingers
                                               .Query()
                                               .Select(f => f.TipPosition.ToVector3())
                                               .Fold((v0, v1) => v0 + v1)
                                               / 5f;
      box0.transform.position = fingertipAvg;
      box0.rigidbody.position = fingertipAvg;
      
      Assert.That(rightHand.TryGrasp(box0) == true);

      yield return wait(endingTestWait);
    }

  }

}
#endif // LEAP_TESTS
