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

  public static class Enum<T> {
    public static readonly string[] names;
    public static readonly T[] values;

    static Enum() {
      names = (string[])Enum.GetNames(typeof(T));
      values = (T[])Enum.GetValues(typeof(T));
    }
  }
}
