/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;

namespace Leap.Unity
{

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]

    public class ExecuteBeforeAttribute : Attribute
    {
        public Type beforeType;
        public ExecuteBeforeAttribute(Type beforeType) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]

    public class ExecuteAfterAttribute : Attribute
    {
        public Type afterType;
        public ExecuteAfterAttribute(Type afterType) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]

    public class ExecuteBeforeDefault : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]

    public class ExecuteAfterDefault : Attribute { }

}