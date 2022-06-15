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

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public class ExecuteBeforeAttribute : Attribute
    {
        public Type beforeType;
        public ExecuteBeforeAttribute(Type beforeType) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public class ExecuteAfterAttribute : Attribute
    {
        public Type afterType;
        public ExecuteAfterAttribute(Type afterType) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public class ExecuteBeforeDefault : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public class ExecuteAfterDefault : Attribute { }

}