using UnityEngine;
using System.Collections;

/// <summary> Allows you to add to a capped-size ring buffer of Vector3s and,
/// when full, compute the buffer's average change over time. </summary>
public class DeltaBuffer {

  private RingBuffer<Vector3> _buffer;
  private RingBuffer<float> _timeBuffer;

  public DeltaBuffer(int bufferSize) {
    _buffer = new RingBuffer<Vector3>(bufferSize);
    _timeBuffer = new RingBuffer<float>(bufferSize);
  }

  public int Length {
    get {
      return _buffer.Length;
    }
  }

  public bool isFull {
    get { return _buffer.isFull; }
  }

  public void Clear() {
    _buffer.Clear();
    _timeBuffer.Clear();
  }

  float _previousSampleTime = 0F;
  public void Add(Vector3 sample, float sampleTime) {
    if (sampleTime == _previousSampleTime) {
      SetLatest(sample, sampleTime);
      return;
    }

    _buffer.Add(sample);
    _timeBuffer.Add(sampleTime);
    _previousSampleTime = sampleTime;
  }

  public Vector3 Get(int idx) {
    return _buffer.Get(idx);
  }

  public Vector3 GetLatest() {
    return Get(Length - 1);
  }

  public void Set(int idx, Vector3 sample, float sampleTime) {
    _buffer.Set(idx, sample);
    _timeBuffer.Set(idx, sampleTime);
  }

  public void SetLatest(Vector3 sample, float sampleTime) {
    Set(Length - 1, sample, sampleTime);
  }

  public float GetTime(int idx) {
    return _timeBuffer.Get(idx);
  }

  /// <summary>Returns the average change between each sample per unit time, or zero if the buffer is not full.</summary>
  public Vector3 Delta() {
    if (!isFull) {
      return Vector3.zero;
    }
    Vector3 deltaPerTimeSum = Vector3.zero;
    int length = Length;
    for (int i = 0; i < length - 1; i++) {
      deltaPerTimeSum += (Get(i + 1) - Get(i)) / (GetTime(i + 1) - GetTime(i));
    }
    return deltaPerTimeSum / (length - 1);
  }

}

/// <summary> Allows you to add to a capped-size ring buffer of floats and,
/// when full, compute the buffer's average change over time. </summary>
public class DeltaFloatBuffer {

  private RingBuffer<float> _buffer;
  private RingBuffer<float> _timeBuffer;

  public DeltaFloatBuffer(int bufferSize) {
    _buffer = new RingBuffer<float>(bufferSize);
    _timeBuffer = new RingBuffer<float>(bufferSize);
  }

  public int Length {
    get {
      return _buffer.Length;
    }
  }

  public bool isFull {
    get { return _buffer.isFull; }
  }

  public void Clear() {
    _buffer.Clear();
    _timeBuffer.Clear();
  }

  float _previousSampleTime = 0F;
  public void Add(float sample, float sampleTime) {
    if (sampleTime == _previousSampleTime) {
      SetLatest(sample, sampleTime);
      return;
    }

    _buffer.Add(sample);
    _timeBuffer.Add(sampleTime);
    _previousSampleTime = sampleTime;
  }

  public float Get(int idx) {
    return _buffer.Get(idx);
  }

  public float GetLatest() {
    return Get(Length - 1);
  }

  public void Set(int idx, float sample, float sampleTime) {
    _buffer.Set(idx, sample);
    _timeBuffer.Set(idx, sampleTime);
  }

  public void SetLatest(float sample, float sampleTime) {
    Set(Length - 1, sample, sampleTime);
  }

  public float GetTime(int idx) {
    return _timeBuffer.Get(idx);
  }

  /// <summary>Returns the average change between each sample per unit time, or zero if the buffer is not full.</summary>
  public float Delta() {
    if (!isFull) {
      return 0F;
    }
    float deltaPerTimeSum = 0F;
    int length = Length;
    for (int i = 0; i < length - 1; i++) {
      deltaPerTimeSum += (Get(i + 1) - Get(i)) / (GetTime(i + 1) - GetTime(i));
    }
    return deltaPerTimeSum / (length - 1);
  }

}