using System;
using UnityEngine;

namespace Leap.Unity.Animation {

  [Serializable]
  public struct CHS {
    public float t0, t1;

    public float pos0, pos1;
    public float vel0, vel1;

    public float PositionAt(float t) {
      float i = Mathf.Clamp01((t - t0) / (t1 - t0));
      float i2 = i * i;
      float i3 = i2 * i;

      float h00 = (2 * i3 - 3 * i2 + 1) * pos0;
      float h10 = (i3 - 2 * i2 + i) * (t1 - t0) * vel0;
      float h01 = (-2 * i3 + 3 * i2) * pos1;
      float h11 = (i3 - i2) * (t1 - t0) * vel1;

      return h00 + h10 + h01 + h11;
    }

    public float VelocityAt(float t) {
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

      float h00_ = (i3_ * 2 - i2_ * 3) * pos0;
      float h10_ = (i3_ - 2 * i2_ + i_) * C00 * vel0;
      float h01_ = (i2_ * 3 - 2 * i3_) * pos1;
      float h11_ = (i3_ - i2_) * C00 * vel1;

      return h00_ + h01_ + h10_ + h11_;
    }

    public void PositionAndVelAt(float t, out float position, out float velocity) {
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

      float h00 = (2 * i3 - 3 * i2 + 1) * pos0;
      float h00_ = (i3_ * 2 - i2_ * 3) * pos0;

      float h10 = (i3 - 2 * i2 + i) * C00 * vel0;
      float h10_ = (i3_ - 2 * i2_ + i_) * C00 * vel0;

      float h01 = (3 * i2 - 2 * i3) * pos1;
      float h01_ = (i2_ * 3 - 2 * i3_) * pos1;

      float h11 = (i3 - i2) * C00 * vel1;
      float h11_ = (i3_ - i2_) * C00 * vel1;

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
