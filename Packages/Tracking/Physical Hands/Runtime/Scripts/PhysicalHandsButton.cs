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
using UnityEngine.Events;


namespace Leap.Unity.PhysicalHands
{
    public class PhysicalHandsButton : MonoBehaviour
    {
        internal const float BUTTON_PRESS_THRESHOLD = 0.01F;
        internal const float BUTTON_PRESS_EXIT_THRESHOLD = 0.09F;

        [SerializeField, Tooltip("The pressable part of the button.")]
        internal GameObject buttonObject;
        [Tooltip("The local position which the button will be limited to and will try to return to.")]
        internal float buttonHeightLimit = 0.02f;


        [SerializeField, Tooltip("Should the button stay down for a time after it is pressed?")]
        internal bool _buttonShouldDelayRebound = false;
        [SerializeField, Tooltip("How long should the button stay down for after it is pressed?")]
        internal float _buttonStaydownTimer = 2;
        [SerializeField, Tooltip("Can this button only be activated by pressing it with a hand?")]
        internal bool _shouldOnlyBePressedByHand = false;
        [SerializeField, Tooltip("Which hand should be able to press this button?")]
        ChiralitySelection _whichHandCanPressButton = ChiralitySelection.BOTH;

        internal bool _isButtonPressed = false;

        internal bool _contactHandPressing = false;
        internal List<Collider> _colliders;

        [Space(10)]
        public UnityEvent OnButtonPressed;
        public UnityEvent OnButtonUnPressed;

        internal bool _leftHandContacting = false;
        internal bool _rightHandContacting = false;

        Coroutine delayedPressCoroutine;

        private void Start()
        {
            _colliders = this.transform.GetComponentsInChildren<Collider>().ToList();
        }


        void FixedUpdate()
        {
            if ((!_isButtonPressed && buttonObject.transform.localPosition.y <= buttonHeightLimit * BUTTON_PRESS_THRESHOLD)
                && (_contactHandPressing || !_shouldOnlyBePressedByHand))
            {
                _isButtonPressed = true;
                ButtonPressed();
            }

            if (_isButtonPressed && buttonObject.transform.localPosition.y >= buttonHeightLimit * BUTTON_PRESS_EXIT_THRESHOLD)
            {
                _isButtonPressed = false;
                ButtonUnpressed();

            }
        }

        private void OnDisable()
        {
            if (delayedPressCoroutine != null)
            {
                StopAllCoroutines();
                delayedPressCoroutine = null;
                EnableButtonMovement();
            }
        }

        void ButtonPressed()
        {
            OnButtonPressed?.Invoke();

            if (_buttonShouldDelayRebound && this.enabled && delayedPressCoroutine == null)
            {
                delayedPressCoroutine = StartCoroutine(ButtonCollisionReset());
            }
        }

        void ButtonUnpressed()
        {
            OnButtonUnPressed?.Invoke();
        }

        IEnumerator ButtonCollisionReset()
        {
            yield return new WaitForFixedUpdate();

            buttonObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;

            foreach (var collider in _colliders)
            {
                collider.enabled = false;
            }

            yield return new WaitForSecondsRealtime(_buttonStaydownTimer);

            EnableButtonMovement();
            delayedPressCoroutine = null;
        }

        void EnableButtonMovement()
        {
            buttonObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;

            foreach (var collider in _colliders)
            {
                collider.enabled = true;
            }
        }

        public void ContactHandNearbyEnter(ContactHand contactHand)
        {
            if (contactHand != null)
            {
                if (contactHand.Handedness == Chirality.Left)
                {
                    _leftHandContacting = true;
                }
                else if (contactHand.Handedness == Chirality.Right)
                {
                    _rightHandContacting = true;
                }
            }

            _contactHandPressing = GetChosenHandInContact();
        }

        public void ContactHandNearbyExit(ContactHand contactHand)
        {
            if (contactHand != null)
            {
                if (contactHand.Handedness == Chirality.Left)
                {
                    _leftHandContacting = false;
                }
                else if (contactHand.Handedness == Chirality.Right)
                {
                    _rightHandContacting = false;
                }
            }
            else
            {
                _leftHandContacting = false;
                _rightHandContacting = false;
            }

            _contactHandPressing = GetChosenHandInContact();
        }

        private bool GetChosenHandInContact()
        {
            switch (_whichHandCanPressButton)
            {
                case ChiralitySelection.LEFT:
                    if (_leftHandContacting)
                    {
                        return true;
                    }
                    break;
                case ChiralitySelection.RIGHT:
                    if (_rightHandContacting)
                    {
                        return true;
                    }
                    break;
                case ChiralitySelection.BOTH:
                    if (_rightHandContacting || _leftHandContacting)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

    }
}