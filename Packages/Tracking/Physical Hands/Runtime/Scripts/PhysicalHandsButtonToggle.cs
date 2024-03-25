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
    public class PhysicalHandsButtonToggle : PhysicalHandsButtonBase
    {
        [Space(10)]
        [Header("Delay Rebound")]
        [Space(5)]
        Vector3 _initialPressableObjectLocalPosition;

        PositionConstraint positionConstraint;

        bool pressingObjectExited = false;

        private void Start()
        {
            _initialPressableObjectLocalPosition = this.transform.localPosition;
            base._buttonHelper._onCollisionEnter += OnCollisionPO;
            base._buttonHelper._onCollisionExit += OnCollisionExitPO;
            base._buttonHelper._onHandContact += OnCollisionPO;
            base._buttonHelper._onHandContactExit += OnCollisionExitPO;
        }

        public void UnPressButton()
        {
            ButtonUnpressed();
        }

        protected override void ButtonPressed()
        {
            base.ButtonPressed();
            _pressableObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            pressingObjectExited = false;
        }

        protected override void ButtonUnpressed()
        {
            base.ButtonUnpressed();

            _pressableObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        }

        private void OnCollisionPO(Collision collision)
        {
            if (!_shouldOnlyBePressedByHand && pressingObjectExited)
            {
                _pressableObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            }
        }

        private void OnCollisionPO(ContactHand contactHand)
        {
            if (GetChosenHandInContact() && pressingObjectExited)
            {
                _pressableObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            }
        }

        private void OnCollisionExitPO(Collision collision)
        {
            if (!_shouldOnlyBePressedByHand)
            {
                StartCoroutine(DelayedCollisionExit());
            }
        }

        private void OnCollisionExitPO(ContactHand contactHand)
        {
            if (GetChosenHandInContact())
            {
                StartCoroutine(DelayedCollisionExit());
            }
        }

        private IEnumerator DelayedCollisionExit()
        {
            // Wait for a short delay before removing constraints
            yield return new WaitForSeconds(1f); // Adjust the delay time as needed

            pressingObjectExited = true;
        }
    }
}
