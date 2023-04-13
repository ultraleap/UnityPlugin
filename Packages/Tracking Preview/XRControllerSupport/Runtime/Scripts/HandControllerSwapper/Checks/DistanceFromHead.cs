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
    /// DistanceFromHead checks to see if the distance of the InputMethodType is less than or equal to the actionThreshold
    /// if lessThan is true, or greater than or equal to the actionThreshold if lessThan is false
    /// </summary>
    public class DistanceFromHead : InputCheckBase
    {
        public Vector3 currentXRControllerPosition;
        public bool lessThan = false;

        private float _distance = 0;

        protected override bool IsTrueLogic()
        {
            Vector3 inputPosition;
            if (GetPosition(out inputPosition))
            {
                _distance = Vector3.Distance(inputPosition, Camera.main.transform.position);

                if (lessThan)
                {
                    return _distance <= actionThreshold;
                }
                else
                {
                    return _distance >= actionThreshold;
                }
            }
            return false;
        }

        private bool GetPosition(out Vector3 inputPosition)
        {
            switch (inputMethodType)
            {
                case InputMethodType.LeapHand:
                    if (_provider.Get(hand) != null)
                    {
                        inputPosition = _provider.Get(hand).PalmPosition;
                        return true;
                    }
                    break;
                case InputMethodType.XRController:
                    if (GetController())
                    {
#if ENABLE_INPUT_SYSTEM
                        inputPosition = _xrController.devicePosition.ReadValue();
#else
                        inputPosition = currentXRControllerPosition;
#endif
                        return true;
                    }
                    break;
            }

            inputPosition = lessThan ? Vector3.positiveInfinity : Vector3.zero;
            return false;
        }
    }
}