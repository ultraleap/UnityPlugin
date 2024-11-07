/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.InputModule
{
    /// <summary>
    /// Defines the set of events supported by the UI input module
    /// </summary>
    public interface IInputModuleEventHandler
    {
        /// <summary>
        /// The event that is triggered upon clicking on a non-canvas UI element.
        /// </summary>
        EventHandler<Vector3> OnClickDown { get; set; }

        /// <summary>
        /// The event that is triggered upon lifting up from a non-canvas UI element (Not 1:1 with onClickDown!)
        /// </summary>
        EventHandler<Vector3> OnClickUp { get; set; }

        /// <summary>
        /// The event that is triggered upon hovering over a non-canvas UI element.
        /// </summary>
        EventHandler<Vector3> OnBeginHover { get; set; }

        /// <summary>
        /// The event that is triggered when the pointer stops hovering over a non-canvas UI element.
        /// </summary>
        EventHandler<Vector3> OnEndHover { get; set; }

        /// <summary>
        /// 
        /// </summary>
        EventHandler<Vector3> OnBeginMissed { get; set; }

        /// <summary>
        /// 
        /// </summary>
        EventHandler<Vector3> OnEndMissed { get; set; }
    }
}