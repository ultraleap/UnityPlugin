/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

#if LEAP_TESTS

using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Leap.Unity.Interaction.Tests {

  public class InteractionButtonTests : InteractionEngineTestBase {
    
    #region Primary Hover Lock

    [UnityTest]
    public IEnumerator CanDeleteButtonWhenPressed() {
      yield return wait(beginningTestWait);

      InitTest(PRESS_BUTTON_RIG, DEFAULT_STAGE);

      // Wait for boxes to rest on the ground.
      yield return wait(aBit);

      // Play the grasping animation.
      recording.Play();

      // Wait for the button to be pressed.

      // (deleteme, older code referencing grasp playback test)
      //bool graspOccurred = false;
      //box0.OnGraspBegin += () => {
      //  graspOccurred = true;
      //};
      //int framesWaited = 0;
      //while (!graspOccurred && framesWaited < WAIT_FOR_INTERACTION_FRAME_LIMIT) {
      //  yield return null;
      //  framesWaited++;
      //}
      //Assert.That(framesWaited != WAIT_FOR_INTERACTION_FRAME_LIMIT);

      //// We should have box0 grasped now.
      //GameObject.Destroy(box0);

      // Primary hover lock should NOT be active once the button is destroyed.

      yield return wait(endingTestWait);
    }

    #endregion

    }

}

#endif // LEAP_TESTS
