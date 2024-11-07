/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using UnityEngine;

namespace Leap.PhysicalHands
{
    /// <summary>
    /// Class for toggling a button using physical hands.
    /// </summary>
    public class PhysicalHandsButtonToggle : PhysicalHandsButton
    {
        [SerializeField]
        private bool _untoggleWhenPressed = true;

        private bool _canUnpress = false;
        private RigidbodyConstraints _constraintsBeforeFreeze = RigidbodyConstraints.None;

        /// <summary>
        /// Indicates whether the button is currently toggled.
        /// </summary>
        public bool IsToggled
        {
            get
            {
                return _isButtonPressed;
            }
        }

        Coroutine delayedUnpressCoroutine;

        private void OnEnable()
        {
            EnableUnpress();
        }

        /// <summary>
        /// Toggles the button state.
        /// </summary>
        /// <param name="shouldFirePressEvents">Indicates if press events should be fired.</param>
        public void ToggleButtonState(bool shouldFirePressEvents = false)
        {
            if (_isButtonPressed == false)
            {
                SetTogglePressed(shouldFirePressEvents);
            }
            else if (_isButtonPressed == true)
            {
                SetToggleUnPressed(shouldFirePressEvents);
            }
        }

        /// <summary>
        /// Sets the button to the pressed state.
        /// </summary>
        /// <param name="shouldFirePressEvents">Indicates if press events should be fired.</param>
        public void SetTogglePressed(bool shouldFirePressEvents = false)
        {
            _isButtonPressed = true;
            _canUnpress = false;

            if (delayedUnpressCoroutine != null)
            {
                StopCoroutine(delayedUnpressCoroutine);
            }

            delayedUnpressCoroutine = StartCoroutine(DelayUnpress());

            _pressableObject.transform.localPosition = new Vector3(_pressableObject.transform.localPosition.x,
                _buttonTravelOffset,
                _pressableObject.transform.localPosition.z);
            FreezeButtonPosition();

            if (shouldFirePressEvents == true)
            {
                OnButtonPressed?.Invoke();
            }
        }

        /// <summary>
        /// Sets the button to the unpressed state.
        /// </summary>
        /// <param name="shouldFirePressEvents">Indicates if press events should be fired.</param>
        public void SetToggleUnPressed(bool shouldFirePressEvents = false)
        {
            _isButtonPressed = false;
            _canUnpress = true;

            _pressableObject.transform.localPosition = new Vector3(_pressableObject.transform.localPosition.x,
                _buttonTravelOffset + _buttonTravelDistanceLocal,
                _pressableObject.transform.localPosition.z);
            UnFreezeButtonPosition();

            if (shouldFirePressEvents == true)
            {
                OnButtonUnPressed?.Invoke();
            }
        }

        /// <summary>
        /// Action to perform when the button is pressed.
        /// </summary>
        protected override void ButtonPressed()
        {
            base.ButtonPressed();
            SetTogglePressed(false);
        }

        /// <summary>
        /// Action to perform when the button is unpressed.
        /// </summary>
        protected override void ButtonUnpressed()
        {
            base.ButtonUnpressed();
            SetToggleUnPressed(false);
        }

        #region Collision handling methods

        /// <summary>
        /// Handles collision events with physics objects.
        /// </summary>
        /// <param name="collision">The collision data.</param>
        protected override void OnCollisionPO(Collision collision)
        {
            base.OnCollisionPO(collision);
            if (_canUnpress && _untoggleWhenPressed)
            {
                // If not exclusively pressed by hand and object has exited
                if (_canBePressedByObjects)
                {
                    UnFreezeButtonPosition();
                }
            }
        }

        /// <summary>
        /// Handles hand contact events with the button.
        /// </summary>
        /// <param name="contactHand">The hand in contact with the button.</param>
        protected override void OnHandContactPO(ContactHand contactHand)
        {
            base.OnHandContactPO(contactHand);
            if (_canUnpress && _untoggleWhenPressed)
            {
                // If the chosen hand is in contact and object has exited
                if (GetChosenHandInContact())
                {
                    UnFreezeButtonPosition();
                }
            }
        }

        /// <summary>
        /// Enables the unpressing of the button.
        /// </summary>
        private void EnableUnpress()
        {
            if (delayedUnpressCoroutine != null)
            {
                StopCoroutine(delayedUnpressCoroutine);
                delayedUnpressCoroutine = null;
            }

            _canUnpress = true;
        }

        /// <summary>
        /// Freezes the position of the button.
        /// </summary>
        private void FreezeButtonPosition()
        {
            _constraintsBeforeFreeze = _pressableObjectRB.constraints;
            _pressableObjectRB.constraints = RigidbodyConstraints.FreezeAll;
        }

        /// <summary>
        /// Unfreezes the position of the button.
        /// </summary>
        private void UnFreezeButtonPosition()
        {
            _pressableObjectRB.constraints = _constraintsBeforeFreeze;
        }

        /// <summary>
        /// Delays the unpressing of the button.
        /// </summary>
        private IEnumerator DelayUnpress()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            _canUnpress = true;
            delayedUnpressCoroutine = null;
        }
        #endregion

    }
}