/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;

namespace Leap.Unity.Attributes {

  [Obsolete]
  public enum AutoFindLocations {
    Object = 0x01,
    Children = 0x02,
    Parents = 0x04,
    Scene = 0x08,
    All = 0xFFFF
  }

  [Obsolete]
  public class AutoFindAttribute : Attribute {
    public readonly AutoFindLocations searchLocations;

    public AutoFindAttribute(AutoFindLocations searchLocations = AutoFindLocations.All) {
      this.searchLocations = searchLocations;
    }
  }
}
