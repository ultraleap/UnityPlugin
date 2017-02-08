using System;
using UnityEngine.Profiling;

public struct ProfilerSample : IDisposable {

  public ProfilerSample(string name) {
    Profiler.BeginSample(name);
  }

  public void Dispose() {
    Profiler.EndSample();
  }
}
