/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Query;
using UnityEngine;

namespace Leap.Unity {

  /// <summary>
  /// Allows you to add to a capped-size ring buffer of Ts and, when full, compute the
  /// buffer's average change over time. DeltaBuffer without type parameters supports
  /// Vector3s; DeltaFloatBuffer supports floats, and DeltaQuaternionBuffer supports
  /// Quaternion rotations.
  /// 
  /// To support other types, subclass DeltaBuffer with your sample type and average
  /// change type (in many cases the these are the same) and implement the Delta()
  /// function to compute the average change of samples currently in the buffer.
  /// </summary>
  public abstract class DeltaBuffer<SampleType, DerivativeType> : IIndexable<SampleType> {

    protected struct ValueTimePair {
      public SampleType value;
      public float time;
    }

    public DeltaBuffer(int bufferSize) {
      _buffer = new RingBuffer<ValueTimePair>(bufferSize);
    }

    protected RingBuffer<ValueTimePair> _buffer;

    public int Count { get { return _buffer.Count; } }

    public bool IsFull { get { return _buffer.IsFull; } }

    public bool IsEmpty { get { return _buffer.IsEmpty; } }

    public int Capacity { get { return _buffer.Capacity; } }

    public SampleType this[int idx] {
      get { return _buffer[idx].value; }
    }

    public void Clear() { _buffer.Clear(); }

    public void Add(SampleType sample, float sampleTime) {
      if (!IsEmpty && sampleTime == GetLatestTime()) {
        SetLatest(sample, sampleTime);
        return;
      }

      _buffer.Add(new ValueTimePair { value = sample, time = sampleTime });
    }

    public SampleType Get(int idx) {
      return _buffer.Get(idx).value;
    }

    public SampleType GetLatest() {
      return Get(Count - 1);
    }

    public void Set(int idx, SampleType sample, float sampleTime) {
      _buffer.Set(idx, new ValueTimePair { value = sample, time = sampleTime });
    }

    public void SetLatest(SampleType sample, float sampleTime) {
      if (Count == 0) Set(0, sample, sampleTime);
      else Set(Count - 1, sample, sampleTime);
    }

    public float GetTime(int idx) {
      return _buffer.Get(idx).time;
    }

    public float GetLatestTime() {
      return _buffer.Get(Count - 1).time;
    }

    /// <summary>
    /// Returns the average change between each sample per unit time.
    /// 
    /// If the buffer is empty, you should return the identity for your derivative type.
    /// </summary>
    public abstract DerivativeType Delta();

    #region foreach Support

    public IndexableEnumerator<SampleType> GetEnumerator() {
      return new IndexableEnumerator<SampleType>(this);
    }

    #endregion

  }

  /// <summary>
  /// A ring buffer of Vector3s with a Delta() function that computes the buffer's
  /// average change over time.
  /// 
  /// The larger the buffer, the more stable but also delayed the resulting average
  /// change over time. A buffer size of 5 is a good start for 60-90 Hz updates.
  /// </summary>
  public class DeltaBuffer : DeltaBuffer<Vector3, Vector3> {

    public DeltaBuffer(int bufferSize) : base(bufferSize) { }

    /// <summary>
    /// Returns the average change between each sample per unit time, or zero if the
    /// buffer contains one or fewer elements.
    /// 
    /// The larger the buffer, the more stable but also delayed the resulting average
    /// change over time. A buffer size of 5 is a good start for 60-90 Hz updates.
    /// </summary>
    public override Vector3 Delta() {
      if (Count <= 1) { return Vector3.zero; }

      Vector3 deltaPerTimeSum = Vector3.zero;
      for (int i = 0; i < Count - 1; i++) {
        deltaPerTimeSum += (Get(i + 1) - Get(i)) / (GetTime(i + 1) - GetTime(i));
      }
      return deltaPerTimeSum / (Count - 1);
    }

  }

  /// <summary>
  /// A ring buffer of floats with a Delta() function that computes the buffer's
  /// average change over time. Delta() will return zero if the buffer contains one
  /// or fewer samples.
  /// 
  /// The larger the buffer, the more stable but also delayed the resulting average
  /// change over time. A buffer size of 5 is a good start for 60-90 Hz updates.
  /// </summary>
  public class DeltaFloatBuffer : DeltaBuffer<float, float> {

    public DeltaFloatBuffer(int bufferSize) : base(bufferSize) { }

    /// <summary>
    /// Returns the average change between each sample per unit time, or zero if the
    /// buffer is empty.
    /// </summary>
    public override float Delta() {
      if (Count <= 1) { return 0f; }

      float deltaPerTimeSum = 0f;
      for (int i = 0; i < Count - 1; i++) {
        deltaPerTimeSum += (Get(i + 1) - Get(i)) / (GetTime(i + 1) - GetTime(i));
      }
      return deltaPerTimeSum / (Count - 1);
    }

  }

  /// <summary>
  /// A ring buffer of Quaternions with a Delta() function that computes the buffer's
  /// average change over time as an angle-axis vector. Returns Vector3.zero if the
  /// buffer contains one or fewer samples.
  /// 
  /// The larger the buffer, the more stable but also delayed the resulting average
  /// change over time. A buffer size of 5 is a good start for 60-90 Hz updates.
  /// </summary>
  public class DeltaQuaternionBuffer : DeltaBuffer<Quaternion, Vector3> {

    public DeltaQuaternionBuffer(int bufferSize) : base(bufferSize) { }

    /// <summary>
    /// Returns the average angular velocity of Quaternions in the buffer as an
    /// angle-axis vector, or zero if the buffer is empty.
    /// </summary>
    public override Vector3 Delta() {
      if (Count <= 1) return Vector3.zero;

      var deltaSum = Vector3.zero;
      for (int i = 0; i < Count - 1; i++) {
        var sample0 = _buffer.Get(i);
        var sample1 = _buffer.Get(i + 1);
        var r0 = sample0.value;
        var t0 = sample0.time;
        var r1 = sample1.value;
        var t1 = sample1.time;

        var delta = (r1.From(r0)).ToAngleAxisVector();
        var deltaTime = t1.From(t0);

        deltaSum += delta / deltaTime;
      }

      return deltaSum / (Count - 1);
    }

  }

}
