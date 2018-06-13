using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Streams {

  public class PoseStreamCatmullRomFilter : MonoBehaviour,
                                            IStreamReceiver<Pose>,
                                            IStream<Pose> {

    public float samplesPerMeter = 256f;
    public float samplesPer90Degrees = 12f;

    public event Action OnOpen  = () => { };
    public event Action<Pose> OnSend = (pose) => { };
    public event Action OnClose = () => { };
    
    private RingBuffer<Pose> _poseBuffer = new RingBuffer<Pose>(4);

    public void Open() {
      _poseBuffer.Clear();

      OnOpen();
    }
    
    private Pose[] _smoothedPosesBuffer = new Pose[1024];

    public void Receive(Pose data) {
      bool wasNotFull = false;
      if (!_poseBuffer.IsFull) wasNotFull = true;

      _poseBuffer.Add(data);

      if (_poseBuffer.IsFull) {
        if (wasNotFull) {
          send(_poseBuffer.Get(0), _poseBuffer.Get(0),
               _poseBuffer.Get(1), _poseBuffer.Get(2));
        }
        send(_poseBuffer.Get(0), _poseBuffer.Get(1),
             _poseBuffer.Get(2), _poseBuffer.Get(3));
      }
    }

    private void send(Pose a, Pose b, Pose c, Pose d, bool reverseOutput = false) {

      var length = Vector3.Distance(b.position,
                                    c.position);
      var numSamplesByPosition = getNumSamplesByPosition(length);

      var ab = b.position - a.position;
      var bc = c.position - b.position;
      var cd = d.position - c.position;
      var angle = Mathf.Max(Vector3.Angle(ab, bc), Vector3.Angle(bc, cd));
      var numSamplesByRotation = getNumSamplesByRotation(angle);

      var numSamples = Mathf.Max(numSamplesByPosition, numSamplesByRotation);

      var spline = Splines.CatmullRom.ToPoseCHS(a, b, c, d);

      var t = 0f;
      var incr = 1f / numSamples;
      var pose = Pose.identity;
      // Note: We do record the position at t = 1, but it's only _used_ when
      // "reverseOutput" is true, which occurs once at the end of the stream.
      for (int i = 0; i <= numSamples; i++) {
        pose = spline.PoseAt(t);

        _smoothedPosesBuffer[i] = pose;

        t += incr;
      }

      if (!reverseOutput) {
        for (int i = 0; i < numSamples; i++) {
          OnSend(_smoothedPosesBuffer[i]);
        }
      }
      else {
        // Starting _at_ numSamples is intentional: This is the very last pose in the
        // stream.
        for (int i = numSamples; i >= 0; i--) {
          OnSend(_smoothedPosesBuffer[i]);
        }
      }
    }

    private int getNumSamplesByPosition(float length) {
      var numSamples = Mathf.FloorToInt(length * samplesPerMeter);
      numSamples = Mathf.Max(2, numSamples);

      return numSamples;
    }

    private int getNumSamplesByRotation(float angle) {
      var numSamples = Mathf.FloorToInt((angle / 90f) * samplesPer90Degrees);
      numSamples = Mathf.Max(2, numSamples);
      return numSamples;
    }

    public void Close() {
      if (_poseBuffer.Count < 2) {
        // Only a single pose: no spline to send, Close() only, below.
      }
      if (_poseBuffer.Count == 2) {
        // Two poses only: Send the two, no spline needed.
        OnSend(_poseBuffer.Get(0));
        OnSend(_poseBuffer.Get(1));
      }
      else if (_poseBuffer.Count == 3) {
        // Edge case: Open, Send 3, Close. Need to send data for the two segments, which
        // we usually handle on the fourth Send.
        send(_poseBuffer.Get(0), _poseBuffer.Get(0),
             _poseBuffer.Get(1), _poseBuffer.Get(2));
        send(_poseBuffer.Get(2), _poseBuffer.Get(2),
             _poseBuffer.Get(1), _poseBuffer.Get(0),
             reverseOutput: true);
      }
      else {
        // General case: With a full buffer, we need to send data to reach the final pose
        // in the stream.
        send(_poseBuffer.Get(3), _poseBuffer.Get(3),
             _poseBuffer.Get(2), _poseBuffer.Get(1),
             reverseOutput: true);
      }

      OnClose();
    }

  }

}
