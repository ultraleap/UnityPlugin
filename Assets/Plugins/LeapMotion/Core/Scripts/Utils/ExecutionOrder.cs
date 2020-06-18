/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;

namespace Leap.Unity {

  [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
  [Obsolete]
  public class ExecuteBeforeAttribute : Attribute {
    public Type beforeType;
    public ExecuteBeforeAttribute(Type beforeType) { }
  }

  [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
  [Obsolete]
  public class ExecuteAfterAttribute : Attribute {
    public Type afterType;
    public ExecuteAfterAttribute(Type afterType) { }
  }

  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
  [Obsolete]
  public class ExecuteBeforeDefault : Attribute { }

  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
  [Obsolete]
  public class ExecuteAfterDefault : Attribute { }

}
