/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
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
