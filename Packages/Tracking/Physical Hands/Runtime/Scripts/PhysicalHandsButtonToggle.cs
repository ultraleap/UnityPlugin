/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;

namespace Leap.Unity.PhysicalHands
{
    /// <summary>
    /// Class for toggling a button using physical hands.
    /// </summary>
    public class PhysicalHandsButtonToggle : PhysicalHandsButtonBase
    {
        private bool _pressingObjectExited = false;

        /// <summary>
        /// Action to perform when the button is pressed.
        /// </summary>
        protected override void ButtonPressed()
        {
            base.ButtonPressed();
            // Freeze object's position and rotation when pressed
            FreezePressableMovement();
            _pressingObjectExited = false;
        }

        /// <summary>
        /// Action to perform when the button is unpressed.
        /// </summary>
        protected override void ButtonUnpressed()
        {
            base.ButtonUnpressed();
            // Remove constraints when unpressed
            UnFreezePressableMovement();
        }

        #region Collision handling methods

        protected override void OnCollisionPO(Collision collision)
        {
            base.OnCollisionPO(collision);
            // If not exclusively pressed by hand and object has exited
            if (!_canBePressedByObjects && _pressingObjectExited)
            {
                UnFreezePressableMovement();
            }
        }

        protected override void OnCollisionPO(ContactHand contactHand)
        {
            base.OnCollisionPO(contactHand);
            // If the chosen hand is in contact and object has exited
            if (GetChosenHandInContact() && _pressingObjectExited)
            {
                UnFreezePressableMovement();
            }
        }

        protected override void OnCollisionExitPO(Collision collision)
        {
            base.OnCollisionExitPO(collision);
            // If not exclusively pressed by hand, start delayed exit
            if (!_canBePressedByObjects)
            {
                StartCoroutine(DelayedCollisionExit());
            }
        }

        protected override void OnCollisionExitPO(ContactHand contactHand)
        {
            base.OnCollisionExitPO(contactHand);
            // Check which hand can press the button and start delayed exit if correct hand is found
            if (contactHand.Handedness == (Chirality)_whichHandCanPressButton)
            {
                StartCoroutine(DelayedCollisionExit());
            }
            else if(_whichHandCanPressButton == ChiralitySelection.BOTH)
            {
                if (!GetChosenHandInContact())
                {
                    StartCoroutine(DelayedCollisionExit());
                }
            }
        }

        /// <summary>
        /// Coroutine for delayed collision exit.
        /// </summary>
        private IEnumerator DelayedCollisionExit()
        {
            // Wait for a short delay before removing constraints
            yield return new WaitForSeconds(1f); // Adjust the delay time as needed

            _pressingObjectExited = true;
        }
        #endregion
    }
}
