using UnityEngine;
using System.Collections;
using System.Diagnostics;

public class FrameLimiter : MonoBehaviour {
  
  IEnumerator Start() {
    Stopwatch stopwatch = new Stopwatch();
    stopwatch.Start();
    long lastTick = stopwatch.ElapsedTicks;
    long ticks = Stopwatch.Frequency * 13 / 1000;
    while (true) {
      yield return null;
      while((stopwatch.ElapsedTicks - lastTick) < ticks) { }
      lastTick = stopwatch.ElapsedTicks;
    }
  }
}
