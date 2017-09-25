using System;
using UnityEngine;

namespace Leap.Unity.Animation {

  public interface ICHS<T> {
    float t0 { get; set; }
    float t1 { get; set; }

    T pos0 { get; set; }
    T pos1 { get; set; }

    T vel0 { get; set; }
    T vel1 { get; set; }

    T PositionAt(float t);
    T VelocityAt(float t);
    void PositionAndVelAt(float t, out T position, out T velocity);
  }

  public static class CHSExtensions {

    public static T RetargetPos<T, K>(this T chs, K position) where T : struct, ICHS<K> {
      return RetargetPos(chs, position, Time.time, Time.time, Time.time + chs.t1 - chs.t0);
    }

    public static T RetargetPos<T, K>(this T chs, K position, float t, float newT0, float newT1) where T : struct, ICHS<K> {
      return Retarget(chs, position, chs.vel1, t, newT0, newT1);
    }

    public static T RetargetVel<T, K>(this T chs, K velocity) where T : struct, ICHS<K> {
      return RetargetVel(chs, velocity, Time.time, Time.time, Time.time + chs.t1 - chs.t0);
    }

    public static T RetargetVel<T, K>(this T chs, K velocity, float t, float newT0, float newT1) where T : struct, ICHS<K> {
      return Retarget(chs, chs.pos1, velocity, t, newT0, newT1);
    }

    public static T Retarget<T, K>(this T chs, K position, K velocity) where T : struct, ICHS<K> {
      return Retarget(chs, position, velocity, Time.time, Time.time, Time.time + chs.t1 - chs.t0);
    }

    public static T Retarget<T, K>(this T chs, K position, K velocity, float t, float newT0, float newT1) where T : struct, ICHS<K> {
      K currPos, currVel;
      chs.PositionAndVelAt(t, out currPos, out currVel);

      chs.t0 = newT0;
      chs.t1 = newT1;

      chs.pos0 = currPos;
      chs.pos1 = position;

      chs.vel0 = currVel;
      chs.vel1 = velocity;

      return chs;
    }
  }

  [Serializable]
  public struct CHS : ICHS<float> {
    private float _t0, _t1;
    private float _pos0, _pos1;
    private float _vel0, _vel1;

    public float t0 {
      get { return _t0; }
      set { _t0 = value; }
    }

    public float t1 {
      get { return _t1; }
      set { _t1 = value; }
    }

    public float pos0 {
      get { return _pos0; }
      set { _pos0 = value; }
    }

    public float pos1 {
      get { return _pos1; }
      set { _pos1 = value; }
    }

    public float vel0 {
      get { return _vel0; }
      set { _vel0 = value; }
    }

    public float vel1 {
      get { return _vel1; }
      set { _vel1 = value; }
    }

    public CHS(float pos0, float pos1) {
      _t0 = 0;
      _t1 = 1;

      _vel0 = 0;
      _vel1 = 0;

      _pos0 = pos0;
      _pos1 = pos1;
    }

    public CHS(float pos0, float pos1, float vel0, float vel1) {
      _t0 = 0;
      _t1 = 1;

      _vel0 = vel0;
      _vel1 = vel1;

      _pos0 = pos0;
      _pos1 = pos1;
    }

    public CHS(float pos0, float pos1, float vel0, float vel1, float length) {
      _t0 = 0;
      _t1 = length;

      _vel0 = vel0;
      _vel1 = vel1;

      _pos0 = pos0;
      _pos1 = pos1;
    }

    public CHS(float t0, float t1, float pos0, float pos1, float vel0, float vel1) {
      _t0 = 0;
      _t1 = 1;

      _vel0 = vel0;
      _vel1 = vel1;

      _pos0 = pos0;
      _pos1 = pos1;
    }

    public float PositionAt(float t) {
      float i = Mathf.Clamp01((t - _t0) / (_t1 - _t0));
      float i2 = i * i;
      float i3 = i2 * i;

      float h00 = (2 * i3 - 3 * i2 + 1) * _pos0;
      float h10 = (i3 - 2 * i2 + i) * (_t1 - _t0) * _vel0;
      float h01 = (-2 * i3 + 3 * i2) * _pos1;
      float h11 = (i3 - i2) * (_t1 - _t0) * _vel1;

      return h00 + h10 + h01 + h11;
    }

    public float VelocityAt(float t) {
      float C00 = _t1 - _t0;
      float C1 = 1.0f / C00;

      float i, i2;
      float i_, i2_, i3_;
      {
        i = Mathf.Clamp01((t - _t0) * C1);
        i_ = C1;

        i2 = i * i;
        i2_ = 2 * i * i_;

        i3_ = i2_ * i + i_ * i2;
      }

      float h00_ = (i3_ * 2 - i2_ * 3) * _pos0;
      float h10_ = (i3_ - 2 * i2_ + i_) * C00 * _vel0;
      float h01_ = (i2_ * 3 - 2 * i3_) * _pos1;
      float h11_ = (i3_ - i2_) * C00 * _vel1;

      return h00_ + h01_ + h10_ + h11_;
    }

    public void PositionAndVelAt(float t, out float position, out float velocity) {
      float C00 = _t1 - _t0;
      float C1 = 1.0f / C00;

      float i, i2, i3;
      float i_, i2_, i3_;
      {
        i = Mathf.Clamp01((t - _t0) * C1);
        i_ = C1;

        i2 = i * i;
        i2_ = 2 * i * i_;

        i3 = i2 * i;
        i3_ = i2_ * i + i_ * i2;
      }

      float h00 = (2 * i3 - 3 * i2 + 1) * _pos0;
      float h00_ = (i3_ * 2 - i2_ * 3) * _pos0;

      float h10 = (i3 - 2 * i2 + i) * C00 * _vel0;
      float h10_ = (i3_ - 2 * i2_ + i_) * C00 * _vel0;

      float h01 = (3 * i2 - 2 * i3) * _pos1;
      float h01_ = (i2_ * 3 - 2 * i3_) * _pos1;

      float h11 = (i3 - i2) * C00 * _vel1;
      float h11_ = (i3_ - i2_) * C00 * _vel1;

      position = h00 + h01 + h10 + h11;
      velocity = h00_ + h01_ + h10_ + h11_;
    }
  }

  [Serializable]
  public struct CHS2 {
    public float t0, t1;

    public Vector2 pos0, pos1;
    public Vector2 vel0, vel1;

    public Vector2 PositionAt(float t) {
      float i = Mathf.Clamp01((t - t0) / (t1 - t0));
      float i2 = i * i;
      float i3 = i2 * i;

      Vector2 h00 = (2 * i3 - 3 * i2 + 1) * pos0;
      Vector2 h10 = (i3 - 2 * i2 + i) * (t1 - t0) * vel0;
      Vector2 h01 = (-2 * i3 + 3 * i2) * pos1;
      Vector2 h11 = (i3 - i2) * (t1 - t0) * vel1;

      return h00 + h10 + h01 + h11;
    }

    public Vector2 VelocityAt(float t) {
      float C00 = t1 - t0;
      float C1 = 1.0f / C00;

      float i, i2;
      float i_, i2_, i3_;
      {
        i = Mathf.Clamp01((t - t0) * C1);
        i_ = C1;

        i2 = i * i;
        i2_ = 2 * i * i_;

        i3_ = i2_ * i + i_ * i2;
      }

      Vector2 h00_ = (i3_ * 2 - i2_ * 3) * pos0;
      Vector2 h10_ = (i3_ - 2 * i2_ + i_) * C00 * vel0;
      Vector2 h01_ = (i2_ * 3 - 2 * i3_) * pos1;
      Vector2 h11_ = (i3_ - i2_) * C00 * vel1;

      return h00_ + h01_ + h10_ + h11_;
    }

    public void PositionAndVelAt(float t, out Vector2 position, out Vector2 velocity) {
      float C00 = t1 - t0;
      float C1 = 1.0f / C00;

      float i, i2, i3;
      float i_, i2_, i3_;
      {
        i = Mathf.Clamp01((t - t0) * C1);
        i_ = C1;

        i2 = i * i;
        i2_ = 2 * i * i_;

        i3 = i2 * i;
        i3_ = i2_ * i + i_ * i2;
      }

      Vector2 h00 = (2 * i3 - 3 * i2 + 1) * pos0;
      Vector2 h00_ = (i3_ * 2 - i2_ * 3) * pos0;

      Vector2 h10 = (i3 - 2 * i2 + i) * C00 * vel0;
      Vector2 h10_ = (i3_ - 2 * i2_ + i_) * C00 * vel0;

      Vector2 h01 = (3 * i2 - 2 * i3) * pos1;
      Vector2 h01_ = (i2_ * 3 - 2 * i3_) * pos1;

      Vector2 h11 = (i3 - i2) * C00 * vel1;
      Vector2 h11_ = (i3_ - i2_) * C00 * vel1;

      position = h00 + h01 + h10 + h11;
      velocity = h00_ + h01_ + h10_ + h11_;
    }
  }

  [Serializable]
  public struct CHS3 {
    public float t0, t1;

    public Vector3 pos0, pos1;
    public Vector3 vel0, vel1;

    public Vector3 PositionAt(float t) {
      float i = Mathf.Clamp01((t - t0) / (t1 - t0));
      float i2 = i * i;
      float i3 = i2 * i;

      Vector3 h00 = (2 * i3 - 3 * i2 + 1) * pos0;
      Vector3 h10 = (i3 - 2 * i2 + i) * (t1 - t0) * vel0;
      Vector3 h01 = (-2 * i3 + 3 * i2) * pos1;
      Vector3 h11 = (i3 - i2) * (t1 - t0) * vel1;

      return h00 + h10 + h01 + h11;
    }

    public Vector3 VelocityAt(float t) {
      float C00 = t1 - t0;
      float C1 = 1.0f / C00;

      float i, i2;
      float i_, i2_, i3_;
      {
        i = Mathf.Clamp01((t - t0) * C1);
        i_ = C1;

        i2 = i * i;
        i2_ = 2 * i * i_;

        i3_ = i2_ * i + i_ * i2;
      }

      Vector3 h00_ = (i3_ * 2 - i2_ * 3) * pos0;
      Vector3 h10_ = (i3_ - 2 * i2_ + i_) * C00 * vel0;
      Vector3 h01_ = (i2_ * 3 - 2 * i3_) * pos1;
      Vector3 h11_ = (i3_ - i2_) * C00 * vel1;

      return h00_ + h01_ + h10_ + h11_;
    }

    public void PositionAndVelAt(float t, out Vector3 position, out Vector3 velocity) {
      float C00 = t1 - t0;
      float C1 = 1.0f / C00;

      float i, i2, i3;
      float i_, i2_, i3_;
      {
        i = Mathf.Clamp01((t - t0) * C1);
        i_ = C1;

        i2 = i * i;
        i2_ = 2 * i * i_;

        i3 = i2 * i;
        i3_ = i2_ * i + i_ * i2;
      }

      Vector3 h00 = (2 * i3 - 3 * i2 + 1) * pos0;
      Vector3 h00_ = (i3_ * 2 - i2_ * 3) * pos0;

      Vector3 h10 = (i3 - 2 * i2 + i) * C00 * vel0;
      Vector3 h10_ = (i3_ - 2 * i2_ + i_) * C00 * vel0;

      Vector3 h01 = (3 * i2 - 2 * i3) * pos1;
      Vector3 h01_ = (i2_ * 3 - 2 * i3_) * pos1;

      Vector3 h11 = (i3 - i2) * C00 * vel1;
      Vector3 h11_ = (i3_ - i2_) * C00 * vel1;

      position = h00 + h01 + h10 + h11;
      velocity = h00_ + h01_ + h10_ + h11_;
    }
  }
}
