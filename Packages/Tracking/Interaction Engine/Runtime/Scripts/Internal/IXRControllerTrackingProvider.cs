/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction
{

    /// <summary>
    /// The interface for providing tracking data to an InteractionVRController.
    /// 
    /// It is recommended that tracking providers update their controllers' tracking
    /// data before the Interaction Manager runs every FixedUpdate to minimize latency.
    /// 
    /// For a reference implementation, refer to DefaultVRNodeTrackingProvider.
    /// </summary>
    public interface IXRControllerTrackingProvider
    {

        /// <summary>
        /// Gets whether or not this provider is currently tracking the controller for which
        /// it provides data.
        /// </summary>
        bool isTracked { get; }

        /// <summary>
        /// An event that is fired whenever new tracking data is available for this
        /// controller.
        /// 
        /// It is recommended that tracking providers fire this event before the Interaction
        /// Manager runs every FixedUpdate to minimize latency.
        /// </summary>
        event Action<Vector3, Quaternion> OnTrackingDataUpdate;

    }

}