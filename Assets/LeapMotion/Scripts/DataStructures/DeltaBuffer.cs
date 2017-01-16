using UnityEngine;

namespace Leap.Unity {

  /// <summary> Allows you to add to a capped-size ring buffer of Vector3s and, when full,
  /// compute the buffer's average change over time. </summary>
  public class DeltaBuffer {

    private Vector3[] arr;
    private float[] timeArr;
    private int firstIdx = 0;
    private int lastIdx = 0;

    public DeltaBuffer(int bufferSize) {
      arr = new Vector3[bufferSize];
      timeArr = new float[bufferSize];
    }

    public int Length {
      get {
        if (lastIdx == -1) return 0;
        int diff = (lastIdx + 1) - firstIdx;
        if (diff <= 0) return diff + arr.Length;
        return diff;
      }
    }

    public bool isFull {
      get { return Length == arr.Length; }
    }

    public void Clear() {
      lastIdx = -1;
      firstIdx = 0;
    }

    float _previousSampleTime = 0F;
    public void Add(Vector3 sample, float sampleTime) {
      if (sampleTime == _previousSampleTime) {
        SetLatest(sample, sampleTime);
        return;
      }

      if (isFull) {
        firstIdx += 1;
        firstIdx %= arr.Length;
      }
      lastIdx += 1;
      lastIdx %= arr.Length;
      
      arr[lastIdx] = sample;
      timeArr[lastIdx] = sampleTime;
      _previousSampleTime = sampleTime;
    }

    public Vector3 Get(int idx) {
      return arr[(firstIdx + idx) % arr.Length];
    }

    public Vector3 GetLatest() {
      return Get(Length - 1);
    }

    public void Set(int idx, Vector3 sample, float sampleTime) {
      int actualIdx = (firstIdx + idx) % arr.Length;
      arr[actualIdx] = sample;
      timeArr[actualIdx] = sampleTime;
    }

    public void SetLatest(Vector3 sample, float sampleTime) {
      Set(Length - 1, sample, sampleTime);
    }

    public float GetTime(int idx) {
      return timeArr[(firstIdx + idx) % arr.Length];
    }

    /// <summary> Returns the average change between each sample per unit time,
    /// or zero if the buffer is not full. </summary>
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

    private float[] arr;
    private float[] timeArr;
    private int firstIdx = 0;
    private int lastIdx = 0;

    public DeltaFloatBuffer(int bufferSize) {
      arr = new float[bufferSize];
      timeArr = new float[bufferSize];
    }

    public int Length {
      get {
        if (lastIdx == -1) return 0;
        int diff = (lastIdx + 1) - firstIdx;
        if (diff <= 0) return diff + arr.Length;
        return diff;
      }
    }

    public bool isFull {
      get { return Length == arr.Length; }
    }

    public void Clear() {
      lastIdx = -1;
      firstIdx = 0;
    }

    float _previousSampleTime = 0F;
    public void Add(float sample, float sampleTime) {
      if (sampleTime == _previousSampleTime) {
        SetLatest(sample, sampleTime);
        return;
      }

      if (isFull) {
        firstIdx += 1;
        firstIdx %= arr.Length;
      }
      lastIdx += 1;
      lastIdx %= arr.Length;

      arr[lastIdx] = sample;
      timeArr[lastIdx] = sampleTime;
      _previousSampleTime = sampleTime;
    }

    public float Get(int idx) {
      return arr[(firstIdx + idx) % arr.Length];
    }

    public float GetLatest() {
      return Get(Length - 1);
    }

    public void Set(int idx, float sample, float sampleTime) {
      int actualIdx = (firstIdx + idx) % arr.Length;
      arr[actualIdx] = sample;
      timeArr[actualIdx] = sampleTime;
    }

    public void SetLatest(float sample, float sampleTime) {
      Set(Length - 1, sample, sampleTime);
    }

    public float GetTime(int idx) {
      return timeArr[(firstIdx + idx) % arr.Length];
    }

    /// <summary> Returns the average change between each sample
    /// per unit time, or zero if the buffer is not full. </summary>
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

}
