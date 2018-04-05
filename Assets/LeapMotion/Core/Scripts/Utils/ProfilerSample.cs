/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEngine.Profiling;

namespace Leap.Unity {

  /// <summary>
  /// A utility struct for ease of use when you want to wrap
  /// a piece of code in a Profiler.BeginSample/EndSample.
  /// Usage:
  /// 
  /// using(new ProfilerSample("Sample Name")) {
  ///   code you want to profile
  /// }
  /// </summary>
  public struct ProfilerSample : IDisposable {

    public ProfilerSample(string sampleName) {
      Profiler.BeginSample(sampleName);
    }

    public ProfilerSample(string sampleName, UnityEngine.Object obj) {
      Profiler.BeginSample(sampleName, obj);
    }

    public void Dispose() {
      Profiler.EndSample();
    }
  }
}
