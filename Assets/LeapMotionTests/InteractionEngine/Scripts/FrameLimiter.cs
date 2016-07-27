using UnityEngine;
using System.Collections;
using System.Diagnostics;

public class FrameLimiter : MonoBehaviour {

  public int frameRate = 75;

  IEnumerator Start() {
    Stopwatch stopwatch = new Stopwatch();
    stopwatch.Start();
    long lastTick = stopwatch.ElapsedTicks;
    long ticks = Stopwatch.Frequency / frameRate;
    while (true) {
      yield return null;
      while((stopwatch.ElapsedTicks - lastTick) < ticks) { }
      lastTick = stopwatch.ElapsedTicks;
    }
  }
}
