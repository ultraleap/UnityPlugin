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

namespace Leap.Unity.PhysicalHands
{
    /// <summary>
    /// Class for toggling a button using physical hands.
    /// </summary>
    public class PhysicalHandsButtonToggle : PhysicalHandsButtonBase
    {
        private bool _canUnpress = false;
        private RigidbodyConstraints _constaintsBeforeFreeze = RigidbodyConstraints.None;

        public bool IsToggled
        {
            get
            {
                return _isButtonPressed;
            }
            set
            {
                _isButtonPressed = value;
            }
        }

        private void OnEnable()
        {
            _canUnpress = true;
        }

        public void ToggleButtonState(bool shouldFirePressEvents = false)
        {
            if (_isButtonPressed == false)
            {
                SetTogglePressed(shouldFirePressEvents);
            }
            else if(_isButtonPressed == true)
            {
                SetToggleUnPressed(shouldFirePressEvents);
            }
        }

        public void SetTogglePressed(bool shouldFirePressEvents = false)
        {
            _isButtonPressed = true;
            _canUnpress = false;
            StartCoroutine(DelayUnpress());

            _pressableObject.transform.localPosition = new Vector3(_pressableObject.transform.localPosition.x,
                _buttonTravelOffset,
                _pressableObject.transform.localPosition.z);
            FreezeButtonPosition();

            if (shouldFirePressEvents == true)
            {
                OnButtonPressed?.Invoke();
            }
        }

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

            // Freeze object's position and rotation when pressed
            SetTogglePressed(false);
        }

        protected override void ButtonUnpressed()
        {
            base.ButtonUnpressed();
            SetToggleUnPressed(false);
        }

        #region Collision handling methods

        protected override void OnCollisionPO(Collision collision)
        {
            base.OnCollisionPO(collision);
            if (_canUnpress)
            {
                // If not exclusively pressed by hand and object has exited
                if (_canBePressedByObjects)
                {
                    UnFreezeButtonPosition();
                }
            }
        }

        protected override void OnHandContactPO(ContactHand contactHand)
        {
            base.OnHandContactPO(contactHand);
            if (_canUnpress)
            {
                // If the chosen hand is in contact and object has exited
                if (GetChosenHandInContact())
                {
                    UnFreezeButtonPosition();
                }
            }
        }

        private void FreezeButtonPosition()
        {
            _constaintsBeforeFreeze = _pressableObjectRB.constraints;
            _pressableObjectRB.constraints = RigidbodyConstraints.FreezeAll;
        }

        private void UnFreezeButtonPosition()
        {
            _pressableObjectRB.constraints = _constaintsBeforeFreeze;
        }

        private IEnumerator DelayUnpress()
        { 
            yield return new WaitForSecondsRealtime(0.5f);
            _canUnpress = true;
        }

        #endregion
    }
}
