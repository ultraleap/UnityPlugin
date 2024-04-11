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
        private bool _canUnpress = false;


        protected override void Start()
        {
            base.Start();
            
        }

        /// <summary>
        /// Action to perform when the button is pressed.
        /// </summary>
        protected override void ButtonPressed()
        {
            base.ButtonPressed();

            // Freeze object's position and rotation when pressed
            _configurableJoint.yMotion = ConfigurableJointMotion.Locked;
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
                    _configurableJoint.yMotion = ConfigurableJointMotion.Limited;
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
                    _configurableJoint.yMotion = ConfigurableJointMotion.Limited;

                }
            }
        }

        private IEnumerator DelayUnpress()
        { 
            yield return new WaitForSecondsRealtime(1f);
            _canUnpress = true;
        }

        #endregion
    }
}
