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
    /// PinchGrasp checks to see if a Hand is pinching or grabbing
    /// </summary>
    public class PinchGrasp : InputCheckBase
    {
        protected override bool IsTrueLogic()
        {
            Hand hand = _provider.GetHand(this.hand);
            if (hand != null)
            {
                if (hand.PinchStrength > actionThreshold || hand.GrabStrength > actionThreshold)
                {
                    return true;
                }
            }
            return false;
        }
    }
}