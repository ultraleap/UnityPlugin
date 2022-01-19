/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using UnityEngine.InputSystem;

namespace Leap.Unity.Controllers
{
    /// <summary>
    /// HasButtonBeenPressed checks to see if a button has been pressed on a controller, if the inputMethodType
    /// is XRController. If it is a LeapHand, this InputCheckBase returns false
    /// </summary>
    public class HasButtonBeenPressed : InputCheckBase
    {

        InputAction anyButton;
        private bool _buttonPressed = false;

        public override void Reset()
        {
            _buttonPressed = false;
            base.Reset();
        }

        public override void Setup(LeapProvider originalProvider)
        {
            string inputaction = $"{hand} HasButtonBeenPressed";
            List<InputAction> actions = InputSystem.ListEnabledActions();
            int ind = actions.FindIndex(x => x.name == inputaction);
            if (ind == -1)
            {
                anyButton = new InputAction(name: inputaction);
                anyButton.AddBinding().WithPath("<XRController>{" + hand + "Hand}/*pressed");
                anyButton.AddBinding().WithPath("<XRController>{" + hand + "Hand}/*touched");
                anyButton.AddBinding().WithPath("<XRController>{" + hand + "Hand}/*clicked");
                anyButton.AddBinding().WithPath("<XRController>{" + hand + "Hand}/<Analog>");

                anyButton.performed += _ => { OnButtonDown(); };
                anyButton.canceled += _ => { OnButtonUp(); };
                anyButton.Enable();
            }
            else
            {
                anyButton = actions[ind];
                anyButton.performed += _ => { OnButtonDown(); };
                anyButton.canceled += _ => { OnButtonUp(); };
            }

            _buttonPressed = false;
            base.Setup(originalProvider);
        }

        protected override bool IsTrueLogic()
        {
            if (GetController())
            {
                return _buttonPressed;
            }
            return false;
        }

        private void OnButtonDown()
        {
            _buttonPressed = true;
        }

        private void OnButtonUp()
        {
            _buttonPressed = false;
        }

    }
}
