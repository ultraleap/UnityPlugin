/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity
{
    public class TrackingMarker : MonoBehaviour
    {
        [Tooltip("The AprilTag marker ID associated with this marker." +
            "\n\nNote: This must be unique within the scene")]
        public int id;
    }
}