/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
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

    public enum ButtonEventType {
      OnPress,
      OnUnpress
    }

    public enum ButtonActionType {
      DisableIt,
      DestroyIt
    }

    [UnityTest]
    public IEnumerator TestButtonActionsDuringEvents(
        [Values(ButtonEventType.OnPress,
                ButtonEventType.OnUnpress)] ButtonEventType buttonEventType,
        [Values(ButtonActionType.DisableIt,
                ButtonActionType.DestroyIt)] ButtonActionType buttonActionType
      ) {

      yield return wait(beginningTestWait);

      InitTest(PRESS_BUTTON_RIG, DEFAULT_STAGE);
      recording.Stop(); // Don't play the recording until we're ready!

      // Wait before starting the test.
      yield return wait(aWhile);

      // Play the button-pressing animation.
      recording.Play();

      // Create the test action to perform when the event is fired.
      System.Action buttonAction;
      switch (buttonActionType) {
        case ButtonActionType.DestroyIt:
          buttonAction = () => { GameObject.Destroy(button); }; break;
        case ButtonActionType.DisableIt:
          buttonAction = () => { button.gameObject.SetActive(false); }; break;
        default:
          throw new System.NotImplementedException("This action is not implemented.");
      }

      // Schedule the test action when the specified button event fires.
      bool eventFired = false;
      System.Action doOnEvent = () => {
        eventFired = true;

        buttonAction();
      };
      switch (buttonEventType) {
        case ButtonEventType.OnPress:
          button.OnPress += doOnEvent; break;
        case ButtonEventType.OnUnpress:
          button.OnUnpress += doOnEvent; break;
        default:
          throw new System.NotImplementedException("This button event is not implemented.");
      }

      // Wait until the button is pressed.
      int framesWaited = 0;
      while (!eventFired && framesWaited < WAIT_FOR_INTERACTION_FRAME_LIMIT) {
        yield return null;
        framesWaited++;
      }
      Assert.That(framesWaited != WAIT_FOR_INTERACTION_FRAME_LIMIT,
                  "Test recording failed to press the button (fire the event).");

      // If the button was disabled or destroyed, the primary hover lock should not
      // be engaged.
      if (buttonActionType == ButtonActionType.DestroyIt
          || buttonActionType == ButtonActionType.DisableIt) {

        Assert.That(rightHand.primaryHoverLocked == false,
                    "Primary hover lock was active even after the button was disabled "
                  + "or destroyed.");
      }

      yield return wait(endingTestWait);
    }

  }

}

#endif // LEAP_TESTS
