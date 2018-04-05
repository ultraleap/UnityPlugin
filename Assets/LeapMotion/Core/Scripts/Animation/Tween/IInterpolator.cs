/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;

namespace Leap.Unity.Animation {

  /// <summary>
  /// Implement this interface to add your own interpolators to Tween!
  /// </summary>
  public interface IInterpolator : IPoolable, IDisposable {

    /// <summary>
    /// Called to trigger the interpolation of this interpolator.  Use
    /// this callback to do whatever work your interpolator needs to do.
    /// </summary>
    void Interpolate(float percent);

    /// <summary>
    /// Returns the 'length' of this interpolator, in whatever units
    /// make sense for this interpolator.  If you are interpolating 
    /// from one point to another, you would return the distance 
    /// between the points.
    /// 
    /// The only current use of this function is to drive the
    /// Tween.AtRate function.
    /// </summary>
    float length { get; }

    /// <summary>
    /// Returns whether or not this interpolator is currently considered
    /// valid.  Any invalid interpolators will be removed from a tween.
    /// </summary>
    bool isValid { get; }
  }
}
