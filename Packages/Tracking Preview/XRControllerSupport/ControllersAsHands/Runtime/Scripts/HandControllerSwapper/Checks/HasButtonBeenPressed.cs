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
using System;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Leap.Unity.Controllers
{
    /// <summary>
    /// HasButtonBeenPressed checks to see if a button has been pressed on a controller, if the inputMethodType
    /// is XRController. If it is a LeapHand, this InputCheckBase returns false
    /// </summary>
    public class HasButtonBeenPressed : InputCheckBase
    {
#if ENABLE_INPUT_SYSTEM
        private InputAction _anyButton;
#endif

        private bool _buttonPressed = false;

        public override void Reset()
        {
            _buttonPressed = false;
            base.Reset();
        }

        public override void Setup(LeapProvider originalProvider)
        {
#if ENABLE_INPUT_SYSTEM
            SetupInputSystem();
#endif

            _buttonPressed = false;
            base.Setup(originalProvider);
        }

#if ENABLE_INPUT_SYSTEM

        private void SetupInputSystem()
        {
            string inputaction = $"{Hand} HasButtonBeenPressed";
            List<InputAction> actions = InputSystem.ListEnabledActions();
            int ind = actions.FindIndex(x => x.name == inputaction);
            if (ind == -1)
            {
                _anyButton = new InputAction(name: inputaction);
                _anyButton.AddBinding().WithPath("<XRController>{" + Hand + "Hand}/*pressed");
                _anyButton.AddBinding().WithPath("<XRController>{" + Hand + "Hand}/*touched");
                _anyButton.AddBinding().WithPath("<XRController>{" + Hand + "Hand}/*clicked");
                _anyButton.AddBinding().WithPath("<XRController>{" + Hand + "Hand}/<Analog>");

                _anyButton.performed += _ => { OnButtonDown(); };
                _anyButton.canceled += _ => { OnButtonUp(); };
                _anyButton.Enable();
            }
            else
            {
                _anyButton = actions[ind];
                _anyButton.performed += _ => { OnButtonDown(); };
                _anyButton.canceled += _ => { OnButtonUp(); };
            }
        }
#endif

        protected override bool IsTrueLogic()
        {
            if (GetController())
            {
#if !ENABLE_INPUT_SYSTEM
                _buttonPressed = IsLegacyXRButtonPressed();
#endif

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

        private bool IsLegacyXRButtonPressed()
        {
            if (Input.GetButton("XRI_" + Hand + "_Primary2DAxisTouch"))
            {
                return true;
            }

            if (Input.GetButton("XRI_" + Hand + "_Primary2DAxisClick"))
            {
                return true;
            }

            if (Input.GetButton("XRI_" + Hand + "_PrimaryButton"))
            {
                return true;
            }

            if (Input.GetButton("XRI_" + Hand + "_SecondaryButton"))
            {
                return true;
            }

            if (Mathf.Abs(Input.GetAxis("XRI_" + Hand + "_Trigger")) > 0)
            {
                return true;
            }

            if (Mathf.Abs(Input.GetAxis("XRI_" + Hand + "_Grip")) > 0)
            {
                return true;
            }

            return false;
        }
    }
}
