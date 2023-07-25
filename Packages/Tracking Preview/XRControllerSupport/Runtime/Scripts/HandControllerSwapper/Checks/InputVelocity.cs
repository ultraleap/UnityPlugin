/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/


using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using System.Collections.Generic;
using UnityEngine.InputSystem;
#endif

namespace Leap.Unity.Controllers
{
    /// <summary>
    /// InputVelocity checks to see if the velocity of the InputMethodType is less than the actionThreshold
    /// if velocityIsLower is true, or greater than the actionThreshold if velocityIsLower is false
    /// </summary>
    public class InputVelocity : InputCheckBase
    {
#if ENABLE_INPUT_SYSTEM
        private InputAction _controllerAction;
#else
        public Vector3 currentVelocity;
#endif

        public bool velocityIsLower = false;
        protected override bool IsTrueLogic()
        {
            Vector3 vel;
            if (GetVelocity(out vel))
            {
                if (velocityIsLower ? vel.magnitude < actionThreshold : vel.magnitude > actionThreshold)
                {
                    return true;
                }
            }
            return false;
        }

        public bool GetVelocity(out Vector3 vel)
        {
            switch (inputMethodType)
            {
                case InputMethodType.LeapHand:
                    if (_provider.GetHand(hand) != null)
                    {
                        vel = _provider.GetHand(hand).PalmPosition;
                        return true;
                    }
                    break;
                case InputMethodType.XRController:
                    if (GetController())
                    {
#if ENABLE_INPUT_SYSTEM
                        vel = _controllerAction.ReadValue<Vector3>();
#else
                        vel = currentVelocity;
#endif
                        return true;
                    }
                    break;
            }
            vel = Vector3.zero;
            return false;
        }

        public override void Setup(LeapProvider originalProvider)
        {
            base.Setup(originalProvider);
#if ENABLE_INPUT_SYSTEM
            SetupInputSystem();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void SetupInputSystem()
        {
            string inputAction = hand.ToString() + " InputVelocityCheck";
            List<InputAction> actions = InputSystem.ListEnabledActions();
            int ind = actions.FindIndex(x => x.name == inputAction);
            if (ind == -1)
            {
                _controllerAction = new InputAction(inputAction);
                _controllerAction.AddBinding().WithPath("<XRController>{" + hand.ToString() + "Hand}/devicePose/velocity");
                _controllerAction.Enable();
            }
            else
            {
                _controllerAction = actions[ind];
            }
        }
#endif
    }
}