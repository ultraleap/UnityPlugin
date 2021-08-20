/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Animation {
  using Internal;

  public partial struct Tween {
    public MaterialSelector Target(Material material) {
      return new MaterialSelector(material, this);
    }
  }
}

namespace Leap.Unity.Animation.Internal {

  public struct MaterialSelector {
    private Material _target;
    private Tween _tween;

    public MaterialSelector(Material target, Tween tween) {
      _target = target;
      _tween = tween;
    }

    #region COLOR
    public Tween Color(Color a, Color b, int propertyId) {
      return _tween.AddInterpolator(Pool<MaterialColorInterpolator>.Spawn().Init(a, b, new MaterialPropertyKey(_target, propertyId)));
    }

    public Tween Color(Color a, Color b, string propertyName = "_Color") {
      return Color(a, b, Shader.PropertyToID(propertyName));
    }

    public Tween Color(Gradient gradient, int propertyId) {
      return _tween.AddInterpolator(Pool<MaterialGradientInterpolator>.Spawn().Init(gradient, new MaterialPropertyKey(_target, propertyId)));
    }

    public Tween Color(Gradient gradient, string propertyName = "_Color") {
      return Color(gradient, Shader.PropertyToID(propertyName));
    }

    public Tween ToColor(Color b, int propertyId) {
      return _tween.AddInterpolator(Pool<MaterialColorInterpolator>.Spawn().Init(_target.GetColor(propertyId), b, new MaterialPropertyKey(_target, propertyId)));
    }

    public Tween ToColor(Color b, string propertyName = "_Color") {
      return ToColor(b, Shader.PropertyToID(propertyName));
    }

    public Tween RGB(Color a, Color b, int propertyId) {
      return _tween.AddInterpolator(Pool<MaterialRGBInterpolator>.Spawn().Init((Vector4)a, (Vector4)b, new MaterialPropertyKey(_target, propertyId)));
    }

    public Tween RGB(Color a, Color b, string propertyName = "_Color") {
      return RGB(a, b, Shader.PropertyToID(propertyName));
    }

    public Tween ToRGB(Color b, int propertyId) {
      return _tween.AddInterpolator(Pool<MaterialRGBInterpolator>.Spawn().Init((Vector4)_target.GetColor(propertyId), (Vector4)b, new MaterialPropertyKey(_target, propertyId)));
    }

    public Tween ToRGB(Color b, string propertyName = "_Color") {
      return ToRGB(b, Shader.PropertyToID(propertyName));
    }

    public Tween Alpha(float a, float b, int propertyId) {
      return _tween.AddInterpolator(Pool<MaterialAlphaInterpolator>.Spawn().Init(a, b, new MaterialPropertyKey(_target, propertyId)));
    }

    public Tween Alpha(float a, float b, string propertyName = "_Color") {
      return Alpha(a, b, Shader.PropertyToID(propertyName));
    }

    public Tween ToAlpha(float b, int propertyId) {
      return _tween.AddInterpolator(Pool<MaterialAlphaInterpolator>.Spawn().Init(_target.GetColor(propertyId).a, b, new MaterialPropertyKey(_target, propertyId)));
    }

    public Tween ToAlpha(float b, string propertyName = "_Color") {
      return ToAlpha(b, Shader.PropertyToID(propertyName));
    }

    private class MaterialColorInterpolator : ColorInterpolatorBase<MaterialPropertyKey> {

      public override void Interpolate(float percent) {
        _target.material.SetColor(_target.propertyId, _a + _b * percent);
      }

      public override void Dispose() {
        _target.material = null;
        Pool<MaterialColorInterpolator>.Recycle(this);
      }

      public override bool isValid { get { return _target.material != null; } }
    }

    private class MaterialGradientInterpolator : GradientInterpolatorBase {

      private MaterialPropertyKey _matPropKey;

      public MaterialGradientInterpolator Init(Gradient gradient, MaterialPropertyKey matPropKey) {
        this._matPropKey = matPropKey;
        Init(gradient);
        return this;
      }

      public override void Interpolate(float percent) {
        _matPropKey.material.SetColor(_matPropKey.propertyId, _gradient.Evaluate(percent));
      }

      public override bool isValid { get { return _matPropKey.material != null; } }
    }

    private class MaterialRGBInterpolator : Vector3InterpolatorBase<MaterialPropertyKey> {

      public override void Interpolate(float percent) {
        float currAlpha = _target.material.GetColor(_target.propertyId).a;
        Color color = (Vector4)(_a + _b * percent);
        color.a = currAlpha;
        _target.material.SetColor(_target.propertyId, color);
      }

      public override void Dispose() {
        _target.material = null;
        Pool<MaterialRGBInterpolator>.Recycle(this);
      }

      public override bool isValid { get { return _target.material != null; } }
    }

    private class MaterialAlphaInterpolator : FloatInterpolatorBase<MaterialPropertyKey> {

      public override void Interpolate(float percent) {
        Color color = _target.material.GetColor(_target.propertyId);
        color.a = Mathf.Lerp(_a, _b, percent);
        _target.material.SetColor(_target.propertyId, color);
      }

      public override void Dispose() {
        _target.material = null;
        Pool<MaterialAlphaInterpolator>.Recycle(this);
      }

      public override bool isValid { get { return _target.material != null; } }
    }
    #endregion

    #region FLOAT
    public Tween Float(float a, float b, int propertyId) {
      return _tween.AddInterpolator(Pool<MaterialFloatInterpolator>.Spawn().Init(a, b, new MaterialPropertyKey(_target, propertyId)));
    }

    public Tween Float(float a, float b, string propertyName) {
      return Float(a, b, Shader.PropertyToID(propertyName));
    }

    public Tween ToFloat(float b, int propertyId) {
      return _tween.AddInterpolator(Pool<MaterialFloatInterpolator>.Spawn().Init(_target.GetFloat(propertyId), b, new MaterialPropertyKey(_target, propertyId)));
    }

    public Tween ToFloat(float b, string propertyName) {
      return ToFloat(b, Shader.PropertyToID(propertyName));
    }

    private class MaterialFloatInterpolator : FloatInterpolatorBase<MaterialPropertyKey> {
      public override void Interpolate(float percent) {
        _target.material.SetFloat(_target.propertyId, _a + _b * percent);
      }

      public override void Dispose() {
        _target.material = null;
        Pool<MaterialFloatInterpolator>.Recycle(this);
      }

      public override bool isValid { get { return _target.material != null; } }
    }
    #endregion

    #region VECTOR
    public Tween Vector(Vector4 a, Vector4 b, int propertyId) {
      return _tween.AddInterpolator(Pool<MaterialVectorInterpolator>.Spawn().Init(a, b, new MaterialPropertyKey(_target, propertyId)));
    }

    public Tween Vector(Vector4 a, Vector4 b, string propertyName) {
      return Vector(a, b, Shader.PropertyToID(propertyName));
    }

    public Tween Vector(Vector3 a, Vector3 b, int propertyId) {
      return _tween.AddInterpolator(Pool<MaterialVectorInterpolator>.Spawn().Init(a, b, new MaterialPropertyKey(_target, propertyId)));
    }

    public Tween Vector(Vector3 a, Vector3 b, string propertyName) {
      return Vector(a, b, Shader.PropertyToID(propertyName));
    }

    public Tween ToVector(Vector4 b, int propertyId) {
      return _tween.AddInterpolator(Pool<MaterialVectorInterpolator>.Spawn().Init(_target.GetVector(propertyId), b, new MaterialPropertyKey(_target, propertyId)));
    }

    public Tween ToVector(Vector4 b, string propertyName) {
      return ToVector(b, Shader.PropertyToID(propertyName));
    }

    public Tween ToVector(Vector3 b, int propertyId) {
      return _tween.AddInterpolator(Pool<MaterialVectorInterpolator>.Spawn().Init(_target.GetVector(propertyId), b, new MaterialPropertyKey(_target, propertyId)));

    }
    public Tween ToVector(Vector3 b, string propertyName) {
      return ToVector(b, Shader.PropertyToID(propertyName));
    }

    private class MaterialVectorInterpolator : Vector4InterpolatorBase<MaterialPropertyKey> {
      public override void Interpolate(float percent) {
        _target.material.SetVector(_target.propertyId, _a + _b * percent);
      }

      public override void Dispose() {
        _target.material = null;
        Pool<MaterialVectorInterpolator>.Recycle(this);
      }

      public override bool isValid { get { return _target.material != null; } }
    }
    #endregion

    #region MATERIAL
    public Tween Material(Material a, Material b) {
      return _tween.AddInterpolator(Pool<MaterialInterpolator>.Spawn().Init(a, b, _target));
    }

    private class MaterialInterpolator : InterpolatorBase<Material, Material> {
      public override float length {
        get {
          return 1;
        }
      }

      public override void Interpolate(float percent) {
        _target.Lerp(_a, _b, percent);
      }

      public override void Dispose() {
        _target = null;
        _a = null;
        _b = null;
        Pool<MaterialInterpolator>.Recycle(this);
      }

      public override bool isValid { get { return _target != null; } }
    }
    #endregion

    private struct MaterialPropertyKey {
      public Material material;
      public int propertyId;

      public MaterialPropertyKey(Material material, int propertyId) {
        this.material = material;
        this.propertyId = propertyId;
      }
    }
  }
}
