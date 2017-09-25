/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;

namespace Leap.Unity.Recording {

  public class RawLeapRecording : LeapRecording {

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

    public override void LoadFrames(List<Frame> frames) {
      frameList.Clear();

      foreach (var frame in frames) {
        var copy = new Frame();
        copy.CopyFrom(frame);
        frameList.Add(copy);
      }
    }

    public override float length {
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

      if (!clampTimeToValid && time < 0 || time > length) {
        return false;
      }

      long timestamp = (long)(time * S_TO_NS) + EarliestTimestamp;

      int index = 0;
      while (index < frameList.Count && frameList[index].Timestamp < timestamp) {
        index++;
      }

      toFill.CopyFrom(frameList[index]);
      return true;
    }
  }
}
