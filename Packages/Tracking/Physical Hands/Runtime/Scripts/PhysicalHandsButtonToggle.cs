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

        private void Start()
        {
            // Subscribe to events
            base._buttonHelper._onCollisionEnter += OnCollisionPO;
            base._buttonHelper._onCollisionExit += OnCollisionExitPO;
            base._buttonHelper._onHandContact += OnCollisionPO;
            base._buttonHelper._onHandContactExit += OnCollisionExitPO;
        }

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

        private new void OnCollisionPO(Collision collision)
        {
            base.OnCollisionPO(collision);
            // If not exclusively pressed by hand and object has exited
            if (!_shouldOnlyBePressedByHand && _pressingObjectExited)
            {
                UnFreezePressableMovement();
            }
        }

        private void OnCollisionPO(ContactHand contactHand)
        {
            // If the chosen hand is in contact and object has exited
            if (GetChosenHandInContact() && _pressingObjectExited)
            {
                UnFreezePressableMovement();
            }
        }

        private new void OnCollisionExitPO(Collision collision)
        {
            base.OnCollisionExitPO(collision);
            // If not exclusively pressed by hand, start delayed exit
            if (!_shouldOnlyBePressedByHand)
            {
                StartCoroutine(DelayedCollisionExit());
            }
        }

        private void OnCollisionExitPO(ContactHand contactHand)
        {
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
