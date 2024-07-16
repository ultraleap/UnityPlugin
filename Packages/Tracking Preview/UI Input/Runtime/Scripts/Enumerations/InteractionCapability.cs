/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap.InputModule
{
    /// <summary>
    /// Defines the interaction modes :
    /// - Both: Both direct and indirect interaction. The active mode depends on the ProjectiveToTactileTransitionDistance value.
    /// - Direct: The user must physically touch the controls.
    /// - Indirect: A cursor is projected from the user's knuckle.
    /// </summary>
    public enum InteractionCapability  // Bitfield might be better here with Hybrid being both bits set ....
    {
        Both,
        Direct,
        Indirect
    };
}