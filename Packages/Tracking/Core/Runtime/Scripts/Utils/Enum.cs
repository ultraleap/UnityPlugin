/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;

namespace Leap.Unity
{
    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public static class Enum<T>
    {
        public static readonly string[] names;
        public static readonly T[] values;

        static Enum()
        {
            names = (string[])Enum.GetNames(typeof(T));
            values = (T[])Enum.GetValues(typeof(T));
        }
    }
}