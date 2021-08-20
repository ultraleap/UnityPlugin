/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
namespace Leap.Unity {
  /// <summary>
  /// Implements a resample-able transform history.
  /// </summary>
  public class TransformHistory {
    public RingBuffer<TransformData> history;
    public TransformHistory(int capacity = 32) {
      history = new RingBuffer<TransformData>(capacity);
    }

    //Store current Transform in History
    public void UpdateDelay(Pose curPose, long timestamp) {
      TransformData currentTransform =
        new TransformData() {
          time = timestamp,
          position = curPose.position,
          rotation = curPose.rotation,
        };

      history.Add(currentTransform);
    }

    //Calculate delayed Transform
    public void SampleTransform(long timestamp, out Vector3 delayedPos, out Quaternion delayedRot) {
      TransformData desiredTransform = TransformData.GetTransformAtTime(history, timestamp);
      delayedPos = desiredTransform.position;
      delayedRot = desiredTransform.rotation;
    }

    public struct TransformData {
      public long time; // microseconds
      public Vector3 position; //meters
      public Quaternion rotation; //magic

      public static TransformData Lerp(TransformData from, TransformData to, long time) {
        if (from.time == to.time) {
          return from;
        }
        float fraction = (float)(((double)(time - from.time)) / ((double)(to.time - from.time)));
        return new TransformData() {
          time = time,
          position = Vector3.Lerp(from.position, to.position, fraction),
          rotation = Quaternion.Slerp(from.rotation, to.rotation, fraction)
        };
      }

      public static TransformData GetTransformAtTime(RingBuffer<TransformData> history, long desiredTime) {
        for (int i = history.Count - 1; i > 0; i--) {
          if (history.Get(i).time >= desiredTime && history.Get(i - 1).time < desiredTime) {
            return Lerp(history.Get(i - 1), history.Get(i), desiredTime);
          }
        }

        if (history.Count > 0) {
          return history.GetLatest();
        }
        else {
          // No history data available.
          return new TransformData() {
            time = desiredTime,
            position = Vector3.zero,
            rotation = Quaternion.identity
          };
        }
      }
    }
  }
}
