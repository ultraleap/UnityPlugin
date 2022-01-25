/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap;
using Leap.Unity;

namespace Leap.Unity.Controllers
{
    /// <summary>
    /// PinchGrasp checks to see if a Hand is pinching or grabbing
    /// </summary>
    public class PinchGrasp : InputCheckBase
    {
        protected override bool IsTrueLogic()
        {
            Hand hand = _provider.Get(this.Hand);
            if (hand != null)
            {
                if (hand.PinchStrength > ActionThreshold || hand.GrabStrength > ActionThreshold)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
