/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap.Unity.PhysicalHands
{
    public class NoContactParent : ContactParent
    {
        internal override void GenerateHands()
        {
            GenerateHandsObjects(typeof(NoContactHand));
        }
    }
}