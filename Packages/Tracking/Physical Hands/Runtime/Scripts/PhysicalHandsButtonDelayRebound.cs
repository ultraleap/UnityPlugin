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
    public class PhysicalHandsButtonDelayRebound : PhysicalHandsButtonBase
    {
        [Space(10)]
        [Header("Delay Rebound")]
        [Space(5)]
        [SerializeField, Tooltip("How long should the button stay down for after it is pressed?")]
        internal float _buttonStaydownTimer = 2;

        protected override void ButtonPressed()
        {
            base.ButtonPressed();

            StartCoroutine(ButtonCollisionReset());
        }


        IEnumerator ButtonCollisionReset()
        {
            yield return new WaitForFixedUpdate();

            this.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;

            foreach (var collider in _colliders)
            {
                collider.enabled = false;
            }

            yield return new WaitForSecondsRealtime(_buttonStaydownTimer);

            this.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;

            foreach (var collider in _colliders)
            {
                collider.enabled = true;
            }
        }
    }
}
