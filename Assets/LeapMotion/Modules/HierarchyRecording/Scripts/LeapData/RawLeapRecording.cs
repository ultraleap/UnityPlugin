using System.Collections.Generic;

namespace Leap.Unity.Recording {

  public class RawLeapRecording : LeapRecording {
    public const double NS_TO_S = 1e-6;
    public const double S_TO_NS = 1e6;

    public List<Frame> frameList = new List<Frame>();

    public long EarliestTimestamp {
      get {
        if (frameList.Count != 0) {
          return frameList[0].Timestamp;
        } else {
          return 0;
        }
      }
    }

    public long LatestTimestamp {
      get {
        if (frameList.Count != 0) {
          return frameList[frameList.Count - 1].Timestamp;
        } else {
          return 0;
        }
      }
    }

    public override float Length {
      get {
        if (frameList.Count != 0) {
          return (float)((frameList[frameList.Count - 1].Timestamp - frameList[0].Timestamp) * NS_TO_S);
        } else {
          return 0;
        }
      }
    }

    public override bool Sample(float time, Frame toFill, bool clampTimeToValid = true) {
      if (frameList.Count == 0) {
        return false;
      }

      if (!clampTimeToValid && time < 0 || time > Length) {
        return false;
      }

      long timestap = (long)(time * S_TO_NS) + EarliestTimestamp;

      int index = 0;
      while (index < frameList.Count && frameList[index].Timestamp > timestap) {
        index++;
      }

      toFill.CopyFrom(frameList[index]);
      return true;
    }
  }
}
