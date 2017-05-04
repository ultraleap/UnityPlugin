/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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
