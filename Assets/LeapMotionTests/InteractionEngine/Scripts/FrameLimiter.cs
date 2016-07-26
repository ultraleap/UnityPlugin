using UnityEngine;
using System.Threading;
using System.Collections;
using System.Diagnostics;

public class FrameLimiter : MonoBehaviour {
  
  IEnumerator Start() {
    Stopwatch stopwatch = new Stopwatch();
    while (true) {
      yield return new WaitForEndOfFrame();
      long delta = 15 - stopwatch.ElapsedMilliseconds;
      if(delta > 0) {
        Thread.Sleep((int)delta);
      }
      
      stopwatch.Reset();
      stopwatch.Start();
    }
  }


}
