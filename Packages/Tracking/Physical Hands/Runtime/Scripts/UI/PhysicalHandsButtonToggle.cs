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
using UnityEngine.Assertions.Must;
using UnityEngine.Events;

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

        protected override void Start()
        {
            base.Start();
        }

        public void ToggleButton(bool shouldFirePressEvents = false)
        {
            if (_isButtonPressed == false)
            {
                MakePressed(shouldFirePressEvents);
            }
            else if(_isButtonPressed == true)
            {
                MakeUnpressed(shouldFirePressEvents);
            }
        }

        public void SetButtonPressed(bool isPressed, bool shouldFirePressEvents = false)
        {
            if (isPressed == true)
            {
                MakePressed(shouldFirePressEvents);
            }
            else if (isPressed == false)
            {
                MakeUnpressed(shouldFirePressEvents);
            }
        }

        private void MakePressed(bool shouldFirePressEvents)
        {
            _isButtonPressed = true;
            _canUnpress = true;
            _pressableObject.transform.localPosition = new Vector3(_pressableObject.transform.localPosition.x,
                0 + _buttonTravelOffset,
                _pressableObject.transform.localPosition.z);
            FreezeButtonPosition();

            if (shouldFirePressEvents == true)
            {
                OnButtonPressed.Invoke();
            }
        }

        private void MakeUnpressed(bool shouldFirePressEvents)
        {
            _isButtonPressed = false;
            _canUnpress = false;
            _pressableObject.transform.localPosition = new Vector3(_pressableObject.transform.localPosition.x,
                _buttonTravelOffset + _buttonTravelDistanceLocal,
                _pressableObject.transform.localPosition.z);
            UnFreezeButtonPosition();

            if (shouldFirePressEvents == true)
            {
                OnButtonUnPressed.Invoke();
            }
        }



        /// <summary>
        /// Action to perform when the button is pressed.
        /// </summary>
        protected override void ButtonPressed()
        {
            base.ButtonPressed();

            // Freeze object's position and rotation when pressed
            FreezeButtonPosition();
            _canUnpress = false;

            StartCoroutine(DelayUnpress());
        }

        protected override void ButtonUnpressed()
        {
            base.ButtonUnpressed();
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
            yield return new WaitForSecondsRealtime(1f);
            _canUnpress = true;
        }

        #endregion
    }
}
