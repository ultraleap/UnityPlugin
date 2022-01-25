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
    /// InputIsInactive checks to see if the assigned InputMethodType is inactive
    /// </summary>
    public class InputIsInactive : InputCheckBase
    {
        protected override bool IsTrueLogic()
        {
            switch (InputMethodType)
            {
                case InputMethodType.LeapHand:
                    return _provider.Get(Hand) == null;
                case InputMethodType.XRController:
                    return !GetController();
            }
            return false;
        }
    }
}