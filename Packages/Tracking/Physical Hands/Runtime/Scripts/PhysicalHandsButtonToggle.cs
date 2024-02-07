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

        private void Start()
        {
            _initialPressableObjectLocalPosition = this.transform.localPosition;
        }

        public void UnPressButton()
        {
            ButtonUnpressed();
        }

        protected override void ButtonPressed()
        {
            base.ButtonPressed();
            this.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        }

        protected override void ButtonUnpressed()
        {
            base.ButtonUnpressed();

            this.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        }
    }
}
