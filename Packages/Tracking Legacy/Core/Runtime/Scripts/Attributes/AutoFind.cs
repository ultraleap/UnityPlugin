/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;

namespace Leap.Unity.Attributes
{

    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public enum AutoFindLocations
    {
        Object = 0x01,
        Children = 0x02,
        Parents = 0x04,
        Scene = 0x08,
        All = 0xFFFF
    }

    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public class AutoFindAttribute : Attribute
    {
        public readonly AutoFindLocations searchLocations;

        public AutoFindAttribute(AutoFindLocations searchLocations = AutoFindLocations.All)
        {
            this.searchLocations = searchLocations;
        }
    }
}