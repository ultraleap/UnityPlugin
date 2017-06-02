/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System;

namespace Leap.Unity.Animation {
  using Internal;

  public partial struct Tween {

    #region FLOAT
    public Tween Value(float a, float b, Action<float> onValue) {
      AddInterpolator(Pool<FloatInterpolator>.Spawn().Init(a, b, onValue));
      return this;
    }

    private class FloatInterpolator : FloatInterpolatorBase<Action<float>> {
      public override void Interpolate(float percent) {
        _target(_a + _b * percent);
      }

      public override void Dispose() {
        _target = null;
        Pool<FloatInterpolator>.Recycle(this);
      }

      public override bool isValid { get { return true; } }
    }
    #endregion

    #region VECTOR2
    public Tween Value(Vector2 a, Vector2 b, Action<Vector2> onValue) {
      AddInterpolator(Pool<Vector2Interpolator>.Spawn().Init(a, b, onValue));
      return this;
    }

    private class Vector2Interpolator : Vector2InterpolatorBase<Action<Vector2>> {
      public override void Interpolate(float percent) {
        _target(_a + _b * percent);
      }

      public override void Dispose() {
        _target = null;
        Pool<Vector2Interpolator>.Recycle(this);
      }

      public override bool isValid { get { return true; } }
    }
    #endregion

    #region VECTOR3
    public Tween Value(Vector3 a, Vector3 b, Action<Vector3> onValue) {
      AddInterpolator(Pool<Vector3Interpolator>.Spawn().Init(a, b, onValue));
      return this;
    }

    private class Vector3Interpolator : Vector3InterpolatorBase<Action<Vector3>> {
      public override void Interpolate(float percent) {
        _target(_a + _b * percent);
      }

      public override void Dispose() {
        _target = null;
        Pool<Vector3Interpolator>.Recycle(this);
      }

      public override bool isValid { get { return true; } }
    }
    #endregion

    #region QUATERNION
    public Tween Value(Quaternion a, Quaternion b, Action<Quaternion> onValue) {
      AddInterpolator(Pool<QuaternionInterpolator>.Spawn().Init(a, b, onValue));
      return this;
    }

    private class QuaternionInterpolator : QuaternionInterpolatorBase<Action<Quaternion>> {
      public override void Interpolate(float percent) {
        _target(Quaternion.Slerp(_a, _b, percent));
      }

      public override void Dispose() {
        _target = null;
        Pool<QuaternionInterpolator>.Recycle(this);
      }

      public override bool isValid { get { return true; } }
    }
    #endregion

    #region COLOR
    public Tween Value(Color a, Color b, Action<Color> onValue) {
      AddInterpolator(Pool<ColorInterpolator>.Spawn().Init(a, b, onValue));
      return this;
    }

    private class ColorInterpolator : ColorInterpolatorBase<Action<Color>> {
      public override void Interpolate(float percent) {
        _target(_a + _b * percent);
      }

      public override void Dispose() {
        _target = null;
        Pool<ColorInterpolator>.Recycle(this);
      }

      public override bool isValid { get { return true; } }
    }
    #endregion
  }
}
