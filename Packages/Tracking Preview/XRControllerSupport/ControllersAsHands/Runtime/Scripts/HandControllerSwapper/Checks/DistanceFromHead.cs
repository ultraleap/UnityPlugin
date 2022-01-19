/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using Leap.Unity;
using UnityEngine;

namespace Leap.Unity.Controllers
{
    /// <summary>
    /// DistanceFromHead checks to see if the distance of the InputMethodType is less than or equal to the actionThreshold
    /// if lessThan is true, or greater than or equal to the actionThreshold if lessThan is false
    /// </summary>
    public class DistanceFromHead : InputCheckBase
    {
        public bool lessThan = false;
        float distance = 0;
        public Vector3 currentXRControllerPosition;
        protected override bool IsTrueLogic()
        {
            Vector3 inputPosition;
            if (GetPosition(out inputPosition))
            {
                distance = Vector3.Distance(inputPosition, MainCameraProvider.mainCamera.transform.position);

                if (lessThan)
                {
                    return distance <= actionThreshold;
                }
                else
                {
                    return distance >= actionThreshold;
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
                        inputPosition = _provider.Get(hand).PalmPosition.ToVector3();
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