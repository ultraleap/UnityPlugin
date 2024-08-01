/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Controllers
{
    /// <summary>
    /// DistanceBetweenInputs checks to see if the distance between inputs (i.e. distance between hands and controllers)
    /// is less than or equal to the actionThreshold if the InputMethodType is LeapHand, or greater than or 
    /// equal to the actionThreshold if the InputMethodType is XRController
    /// </summary>
    public class DistanceBetweenInputs : InputCheckBase
    {
        public Vector3 currentXRControllerPosition;

        protected override bool IsTrueLogic()
        {
            if (GetController() && _provider.GetHand(hand) != null)
            {
                if (InputDistanceCheck())
                {
                    return true;
                }
            }
            return false;
        }

        protected bool InputDistanceCheck()
        {
            Vector3 xrControllerPosition = Vector3.zero;
#if ENABLE_INPUT_SYSTEM
            xrControllerPosition = _xrController.devicePosition.ReadValue();
#else
            xrControllerPosition = currentXRControllerPosition;
#endif

            if (inputMethodType == InputMethodType.LeapHand)
            {
                if (Vector3.Distance(_provider.GetHand(hand).PalmPosition, xrControllerPosition) <= actionThreshold)
                {
                    return true;
                }
            }
            else
            {
                if (Vector3.Distance(_provider.GetHand(hand).PalmPosition, xrControllerPosition) >= actionThreshold)
                {
                    return true;
                }
            }
            return false;
        }

    }
}