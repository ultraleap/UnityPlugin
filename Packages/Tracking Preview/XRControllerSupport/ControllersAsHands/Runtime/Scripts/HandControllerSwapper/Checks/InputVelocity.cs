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
    /// InputVelocity checks to see if the velocity of the InputMethodType is less than the actionThreshold
    /// if velocityIsLower is true, or greater than the actionThreshold if velocityIsLower is false
    /// </summary>
    public class InputVelocity : InputCheckBase
    {
        InputAction _controllerAction;

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
                    if (_provider.Get(hand) != null)
                    {
                        vel = _provider.Get(hand).PalmPosition.ToVector3();
                        return true;
                    }
                    break;
                case InputMethodType.XRController:
                    if (GetController())
                    {
                        vel = _controllerAction.ReadValue<Vector3>();
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
            string inputaction = hand.ToString() + " InputVelocityCheck";
            List<InputAction> actions = InputSystem.ListEnabledActions();
            int ind = actions.FindIndex(x => x.name == inputaction);
            if (ind == -1)
            {
                _controllerAction = new InputAction(inputaction);
                _controllerAction.AddBinding().WithPath("<XRController>{" + hand.ToString() + "Hand}/devicePose/velocity");
                _controllerAction.Enable();
            }
            else
            {
                _controllerAction = actions[ind];
            }
        }
    }
}