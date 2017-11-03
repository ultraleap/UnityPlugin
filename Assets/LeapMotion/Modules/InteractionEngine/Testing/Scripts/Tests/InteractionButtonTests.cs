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
    public IEnumerator CanDestroyButtonWhenPressed() {
      yield return wait(beginningTestWait);

      InitTest(PRESS_BUTTON_RIG, DEFAULT_STAGE);

      // Wait before starting the test.
      yield return wait(aWhile);

      // Play the grasping animation.
      recording.Play();

      // Schedule the deletion of the button when the button is pressed.
      bool pressed = false;
      button.OnPress += () => {
        pressed = true;

        GameObject.Destroy(button);
      };

      // Wait until the button is pressed.
      int framesWaited = 0;
      while (!pressed && framesWaited < WAIT_FOR_INTERACTION_FRAME_LIMIT) {
        yield return null;
        framesWaited++;
      }
      Assert.That(framesWaited != WAIT_FOR_INTERACTION_FRAME_LIMIT);

      // Primary hover lock should NOT be active once the button is destroyed.
      Assert.That(rightHand.primaryHoverLocked == false);

      yield return wait(endingTestWait);
    }

    [UnityTest]
    public IEnumerator CanDestroyButtonWhenUnpressed() {
      yield return wait(beginningTestWait);

      InitTest(PRESS_BUTTON_RIG, DEFAULT_STAGE);

      // Wait before starting the test.
      yield return wait(aWhile);

      // Play the grasping animation.
      recording.Play();

      // Schedule the deletion of the button when the button is pressed.
      bool unpressed = false;
      button.OnUnpress += () => {
        unpressed = true;

        GameObject.Destroy(button);
      };

      // Wait until the button is pressed.
      int framesWaited = 0;
      while (!unpressed && framesWaited < WAIT_FOR_INTERACTION_FRAME_LIMIT) {
        yield return null;
        framesWaited++;
      }
      Assert.That(framesWaited != WAIT_FOR_INTERACTION_FRAME_LIMIT);

      // Primary hover lock should NOT be active once the button is destroyed.
      Assert.That(rightHand.primaryHoverLocked == false);

      yield return wait(endingTestWait);
    }

    [UnityTest]
    public IEnumerator CanDisableButtonWhenPressed() {
      yield return wait(beginningTestWait);

      InitTest(PRESS_BUTTON_RIG, DEFAULT_STAGE);

      // Wait before starting the test.
      yield return wait(aWhile);

      // Play the grasping animation.
      recording.Play();

      // Schedule the deletion of the button when the button is pressed.
      bool pressed = false;
      button.OnPress += () => {
        pressed = true;

        button.gameObject.SetActive(false);
      };

      // Wait until the button is pressed.
      int framesWaited = 0;
      while (!pressed && framesWaited < WAIT_FOR_INTERACTION_FRAME_LIMIT) {
        yield return null;
        framesWaited++;
      }
      Assert.That(framesWaited != WAIT_FOR_INTERACTION_FRAME_LIMIT);

      // Primary hover lock should NOT be active once the button is destroyed.
      Assert.That(rightHand.primaryHoverLocked == false);

      yield return wait(endingTestWait);
    }

    [UnityTest]
    public IEnumerator CanDisableButtonWhenUnpressed() {
      yield return wait(beginningTestWait);

      InitTest(PRESS_BUTTON_RIG, DEFAULT_STAGE);

      // Wait before starting the test.
      yield return wait(aWhile);

      // Play the grasping animation.
      recording.Play();

      // Schedule the deletion of the button when the button is pressed.
      bool unpressed = false;
      button.OnUnpress += () => {
        unpressed = true;

        button.gameObject.SetActive(false);
      };

      // Wait until the button is pressed.
      int framesWaited = 0;
      while (!unpressed && framesWaited < WAIT_FOR_INTERACTION_FRAME_LIMIT) {
        yield return null;
        framesWaited++;
      }
      Assert.That(framesWaited != WAIT_FOR_INTERACTION_FRAME_LIMIT);

      // Primary hover lock should NOT be active once the button is destroyed.
      Assert.That(rightHand.primaryHoverLocked == false);

      yield return wait(endingTestWait);
    }

    #endregion

  }

}

#endif // LEAP_TESTS
