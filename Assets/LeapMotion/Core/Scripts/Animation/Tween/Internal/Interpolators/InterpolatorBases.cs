/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System;

namespace Leap.Unity.Animation.Internal {

  public abstract class InterpolatorBase<ValueType, ObjType> : IInterpolator {
    protected ValueType _a, _b;
    protected ObjType _target;

    public InterpolatorBase<ValueType, ObjType> Init(ValueType a, ValueType b, ObjType target) {
      _a = a;
      _b = b;
      _target = target;
      return this;
    }

    public abstract float length { get; }

    public abstract void Interpolate(float percent);

    public abstract bool isValid { get; }

    public void OnSpawn() { }

    public void OnRecycle() { }

    public abstract void Dispose();
  }

  public abstract class FloatInterpolatorBase<ObjType> : InterpolatorBase<float, ObjType> {
    public override float length {
      get {
        return Mathf.Abs(_b);
      }
    }

    public new FloatInterpolatorBase<ObjType> Init(float a, float b, ObjType target) {
      _a = a;
      _b = b - a;
      _target = target;
      return this;
    }
  }

  public abstract class Vector2InterpolatorBase<ObjType> : InterpolatorBase<Vector2, ObjType> {
    public override float length {
      get {
        return _b.magnitude;
      }
    }

    public new Vector2InterpolatorBase<ObjType> Init(Vector2 a, Vector2 b, ObjType target) {
      _a = a;
      _b = b - a;
      _target = target;
      return this;
    }
  }

  public abstract class Vector3InterpolatorBase<ObjType> : InterpolatorBase<Vector3, ObjType> {
    public override float length {
      get {
        return _b.magnitude;
      }
    }

    public new Vector3InterpolatorBase<ObjType> Init(Vector3 a, Vector3 b, ObjType target) {
      _a = a;
      _b = b - a;
      _target = target;
      return this;
    }
  }

  public abstract class Vector4InterpolatorBase<ObjType> : InterpolatorBase<Vector4, ObjType> {
    public override float length {
      get {
        return _b.magnitude;
      }
    }

    public new Vector4InterpolatorBase<ObjType> Init(Vector4 a, Vector4 b, ObjType target) {
      _a = a;
      _b = b - a;
      _target = target;
      return this;
    }
  }

  public abstract class QuaternionInterpolatorBase<ObjType> : InterpolatorBase<Quaternion, ObjType> {
    public override float length {
      get {
        return Quaternion.Angle(_a, _b);
      }
    }

    public new QuaternionInterpolatorBase<ObjType> Init(Quaternion a, Quaternion b, ObjType target) {
      _a = a;
      _b = b;
      _target = target;
      return this;
    }
  }

  public abstract class ColorInterpolatorBase<ObjType> : InterpolatorBase<Color, ObjType> {
    public override float length {
      get {
        return ((Vector4)_b).magnitude;
      }
    }

    public new ColorInterpolatorBase<ObjType> Init(Color a, Color b, ObjType target) {
      _a = a;
      _b = b - a;
      _target = target;
      return this;
    }
  }

  public abstract class GradientInterpolatorBase : IInterpolator {

    protected Gradient _gradient;

    public GradientInterpolatorBase Init(Gradient gradient) {
      _gradient = gradient;
      return this;
    }

    public float length { get { return 1; } }

    public abstract void Interpolate(float percent);
    public abstract bool isValid { get; }

    public void OnSpawn() { }

    public void Dispose() { }

    public void OnRecycle() { }
  }

}
