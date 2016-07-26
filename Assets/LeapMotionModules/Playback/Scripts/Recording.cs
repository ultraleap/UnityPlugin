using UnityEngine;
using System.Collections.Generic;

namespace Leap.Unity.Playback {

  public class Recording : ScriptableObject {
    public List<Frame> frames = new List<Frame>();

    public virtual void TrimStartOfEmptyFrames(int framesToRemain) {
      int firstFrameWithHand = -1;

      for (int i = 0; i < frames.Count; i++) {
        if (frames[i].Hands.Count != 0) {
          firstFrameWithHand = i;
          break;
        }
      }

      int trimStart;
      if (firstFrameWithHand == -1) {
        trimStart = framesToRemain;
      } else {
        trimStart = Mathf.Max(0, firstFrameWithHand - framesToRemain);
      }

      TrimStart(trimStart);
    }

    public virtual void TrimEndOfEmptyFrames(int framesToRemain) {
      int lastFrameWithHand = -1;

      for (int i = frames.Count - 1; i >= 0; i--) {
        if (frames[i].Hands.Count != 0) {
          lastFrameWithHand = i;
          break;
        }
      }

      int trimEnd;
      if (lastFrameWithHand == -1) {
        trimEnd = framesToRemain;
      } else {
        trimEnd = Mathf.Max(0, (frames.Count - 1 - lastFrameWithHand) - framesToRemain);
      }

      TrimEnd(trimEnd);
    }

    public virtual void TrimStart(int trimCount) {
      for (int i = 0; i < trimCount; i++) {
        frames.RemoveAt(0);
      }
    }

    public virtual void TrimEnd(int trimCount) {
      for (int i = 0; i < trimCount; i++) {
        frames.RemoveAt(frames.Count - 1);
      }
    }
  }
}
