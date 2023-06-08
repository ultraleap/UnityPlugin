/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap.Unity.Controllers
{
    /// <summary>
    /// InputIsInactive checks to see if the assigned InputMethodType is inactive
    /// </summary>
    public class InputIsInactive : InputCheckBase
    {
        protected override bool IsTrueLogic()
        {
            switch (inputMethodType)
            {
                case InputMethodType.LeapHand:
                    return _provider.GetHand(hand) == null;
                case InputMethodType.XRController:
                    return !GetController();
            }
            return false;
        }
    }
}