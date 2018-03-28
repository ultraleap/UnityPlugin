/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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
