using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Streams {

  public class PoseStreamMovingAverageFilter : MonoBehaviour,
                                               IStreamReceiver<Pose>,
                                               IStream<Pose> {

    [Range(0, 16)]
    public int neighborRadius = 4;

    public event Action OnOpen  = () => { };
    public event Action<Pose> OnSend = (pose) => { };
    public event Action OnClose = () => { };

    private RingBuffer<Pose> buffer;

    private int totalWidth { get { return neighborRadius * 2 + 1; } }

    public void Open() {
      if (buffer == null || buffer.Capacity != totalWidth) {
        buffer = new RingBuffer<Pose>(totalWidth);
      }
      buffer.Clear();

      OnOpen();
    }

    public void Receive(Pose data) {
      if (buffer.Capacity == 0) {
        OnSend(data);
      }

      bool bufferWasNotFull = false;
      if (!buffer.IsFull) {
        bufferWasNotFull = true;
      }

      buffer.Add(data);

      if (buffer.IsFull) {
        if (bufferWasNotFull) {
          for (int i = 1; i < buffer.Count; i += 2) {
            OnSend(getAverage(0, i));
          }
        }

        OnSend(getAverage(0, buffer.Count));
      }
    }

    private Pose getAverage(int start, int end) {
      if (start == end) return buffer.Get(start);

      var sum = Vector3.zero;
      for (int i = start; i < end; i++) {
        sum += buffer.Get(i).position;
      }
      var avgPos = sum / (end - start);

      // Try a fractionally-slerped accumulation for "averaging" the rotations.
      var rot = buffer.Get(start).rotation;
      var div = 1 / (end - start);
      for (int i = start + 1; i < end; i++) {
        rot = Quaternion.Slerp(rot, buffer.Get(i).rotation, div);
      }

      return new Pose(avgPos, rot);
    }

    public void Close() {
      var finalPose = buffer.GetLatest();

      if (Application.isPlaying) {
        for (int i = 0; i < buffer.Count - 1; i++) {
          Receive(finalPose);
        }
      }
      else {
        // Just pass-through if we're getting a value at edit-time.
        OnSend(finalPose);
      }

      OnClose();
    }

  }

}
