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

namespace Leap.Unity.Controllers
{
    public class DistanceBetweenInputs : InputCheckBase
    {

        protected override bool IsTrueLogic()
        {
            if (GetController() && _provider.Get(hand) != null)
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
            if (inputMethodType == InputMethodType.LeapHand)
            {
                if (Vector3.Distance(_provider.Get(hand).PalmPosition.ToVector3(), _xrController.devicePosition.ReadValue()) <= actionThreshold)
                {
                    return true;
                }
            }
            else
            {
                if (Vector3.Distance(_provider.Get(hand).PalmPosition.ToVector3(), _xrController.devicePosition.ReadValue()) >= actionThreshold)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
