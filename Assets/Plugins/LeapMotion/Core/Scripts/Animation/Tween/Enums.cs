/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap.Unity.Animation {

  public enum Direction {
    Forward = 1,
    Backward = -1
  }

  public enum SmoothType {
    Linear = 1,
    Smooth = 2,
    SmoothEnd = 3,
    SmoothStart = 4
  }
}
