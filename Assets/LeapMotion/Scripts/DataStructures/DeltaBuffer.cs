/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections;

/// <summary> Allows you to add to a capped-size ring buffer of Ts and,
/// when full, compute the buffer's average change over time.
/// DeltaBuffer without type parameters supports Vector3s;
/// DeltaFloatBuffer supports floats. To support other types,
/// subclass DeltaBuffer<YourType> and implement its Delta() averaging function.</summary>
public abstract class DeltaBuffer<T> {

  protected struct ValueTimePair {
    public T value;
    public float time;
  }

  public DeltaBuffer(int bufferSize) {
    _buffer = new RingBuffer<ValueTimePair>(bufferSize);
  }

  protected RingBuffer<ValueTimePair> _buffer; 

  public int  Length { get { return _buffer.Length; } }
  public bool IsFull { get { return _buffer.IsFull; } }

  public void Clear() { _buffer.Clear(); }

  private float _previousSampleTime = 0F;
  public void Add(T sample, float sampleTime) {
    if (sampleTime == _previousSampleTime) {
      SetLatest(sample, sampleTime);
      return;
    }

    _buffer.Add(new ValueTimePair { value = sample, time = sampleTime });
  }

  public T Get(int idx) {
    return _buffer.Get(idx).value;
  }

  public T GetLatest() {
    return Get(Length - 1);
  }

  public void Set(int idx, T sample, float sampleTime) {
    _buffer.Set(idx, new ValueTimePair { value = sample, time = sampleTime });
  }

  public void SetLatest(T sample, float sampleTime) {
    Set(Length - 1, sample, sampleTime);
  }

  public float GetTime(int idx) {
    return _buffer.Get(idx).time;
  }

  /// <summary> Returns the average change between each sample per unit time, or zero if the buffer is not full. </summary>
  public abstract T Delta();

}

/// <summary> Allows you to add to a capped-size ring buffer of Vector3s and,
/// when full, compute the buffer's average change over time. </summary>
public class DeltaBuffer : DeltaBuffer<Vector3> {

  public DeltaBuffer(int bufferSize) : base(bufferSize) { }

  /// <summary> Returns the average change between each sample per unit time, or zero if the buffer is not full. </summary>
  public override Vector3 Delta() {
    if (!IsFull) {
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
public class DeltaFloatBuffer : DeltaBuffer<float> {

  public DeltaFloatBuffer(int bufferSize) : base(bufferSize) { }

  /// <summary>Returns the average change between each sample per unit time, or zero if the buffer is not full.</summary>
  public override float Delta() {
    if (!IsFull) {
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
