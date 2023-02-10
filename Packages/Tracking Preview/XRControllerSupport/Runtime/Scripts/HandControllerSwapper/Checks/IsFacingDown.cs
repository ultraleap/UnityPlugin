/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Controllers
{
    /// <summary>
    /// IsFacingDown checks to see if the angle between the InputMethodType and the floor 
    /// is less than the action threshold. This is useful for checking if XRControllers
    /// are dangling from a user's wrists.
    /// </summary>
    public class IsFacingDown : InputCheckBase
    {
        public Quaternion currentXRControllerRotation;

        protected override bool IsTrueLogic()
        {
            float angle;
            if (GetAngleBetweenInputAndFloor(out angle))
            {
                if (angle <= actionThreshold)
                {
                    return true;
                }
            }
            return false;
        }

        private bool GetAngleBetweenInputAndFloor(out float angle)
        {
            switch (inputMethodType)
            {
                case InputMethodType.LeapHand:
                    if (_provider.Get(hand) != null)
                    {
                        angle = Vector3.Angle(_provider.Get(hand).Direction, Vector3.down);
                        return true;
                    }
                    break;
                case InputMethodType.XRController:
                    if (GetController())
                    {
#if ENABLE_INPUT_SYSTEM
                        angle = Vector3.Angle(_xrController.deviceRotation.ReadValue() * Vector3.forward, Vector3.down);
#else
                        angle = Vector3.Angle(currentXRControllerRotation * Vector3.forward, Vector3.down);
#endif
                        return true;
                    }
                    break;
            }

            angle = Mathf.Infinity;
            return false;
        }
    }
}