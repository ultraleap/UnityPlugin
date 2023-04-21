/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap.Unity.InputModule
{
    /// <summary>
    /// Enumeration of states that the pointer can be in
    /// </summary>
    public enum PointerStates
    {
        OnCanvas,
        OnElement,
        PinchingToCanvas,
        PinchingToElement,
        NearCanvas,
        TouchingCanvas,
        TouchingElement,
        OffCanvas
    };
}