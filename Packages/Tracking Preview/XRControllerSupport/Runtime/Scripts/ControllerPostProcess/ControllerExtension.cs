/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

#if ENABLE_INPUT_SYSTEM

using UnityEngine.InputSystem;
namespace Leap.Controllers
{
    public static class ControllerExtension
    {
        /// <summary>
        /// Returns whether a controller is active based on if the controller is null,
        /// if the controller has been added and if the controller is tracked
        /// </summary>
        public static bool IsControllerActive(this TrackedDevice controller)
        {
            return controller != null && controller.added && controller.isTracked.ReadValue() == 1;
        }
    }
}
#endif