/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using Leap.Unity.Encoding;
using Leap.Unity.Infix;
using UnityEngine;

namespace Leap.Unity.Geometry {

  using UnityRect = UnityEngine.Rect;

  [System.Serializable]
  public struct LocalRect : IInterpolable<LocalRect> {

    public Vector3 center;
    public Vector2 radii;

    public static readonly LocalRect unit =
      new LocalRect(Vector3.zero, new Vector2(0.5f, 0.5f));

    public LocalRect(Vector3 center, Vector2 radii) {
      this.center = center;
      this.radii = radii;
    }

    public LocalRect(Vector2 radii) : this(default(Vector3), radii) { }

    public LocalRect(float top, float bottom, float left, float right) {
      this.center = new Vector3((left + right) / 2f, (top + bottom) / 2f, 0f);
      var width = Mathf.Abs(right - left);
      var height = Mathf.Abs(top - bottom);
      this.radii = new Vector2(width / 2f, height / 2f);
    }

    public LocalRect(UnityRect uRect) {
      this.center = uRect.center;
      this.radii = new Vector2(uRect.width / 2f, uRect.height / 2f);
    }

    public static LocalRect FromUnityRect(UnityRect uRect) {
      return new LocalRect(uRect.center, new Vector2(uRect.width / 2f,
        uRect.height / 2f));
    }

    public static LocalRect FromRadius(float boxRadius) {
      return new LocalRect(Vector3.zero, Vector2.one * boxRadius);
    }

    public Vector3 corner00 { get { return center - radii.WithZ(0f); } }
    public Vector3 corner10 { get {
      return center + radii.CompMul(new Vector2(-1f, 1f)).WithZ(0f); }
    }

    public float top { get { return center.y + radii.y; } }
    public float bottom { get { return center.y - radii.y; } }
    public float left { get { return center.x - radii.x; } }
    public float right { get { return center.x + radii.x; } }
    public float width { get { return radii.x * 2f; } }
    public float height { get { return radii.y * 2f; } }

    public Rect With(Transform transform) {
      return new Rect(this, transform);
    }

    public UnityRect ToUnityRect() {
      return new UnityRect(center.ToVector2() - radii, radii * 2f);
    }

    /// <summary> Returns a new LocalRect with scaled radii. The center is
    /// unchanged. </summary>
    public LocalRect Scale(float radiusMultipler) {
      return new LocalRect(this.center, this.radii * radiusMultipler);
    }

    /// <summary> Returns a new LocalRect with component-wise scaled radii.
    /// The center is unchanged. </summary>
    public LocalRect Scale(Vector2 radiusMultiplers) {
      return new LocalRect(this.center, this.radii.CompMul(radiusMultiplers));
    }

    /// <summary> Returns a new LocalRect whose center matches the center of the
    /// argument rect. </summary>
    public LocalRect AlignToCenter(LocalRect ofRect) {
      return new LocalRect(ofRect.center, this.radii);
    }

    public LocalRect CopyFrom(LocalRect toCopy) {
      center = toCopy.center;
      radii = toCopy.radii;
      return this;
    }

    public bool FillLerped(LocalRect from, LocalRect to, float t) {
      center = Vector3.Lerp(from.center, to.center, t);
      radii = Vector2.Max(Vector2.Lerp(from.radii, to.radii, t), Vector2.zero);
      return true;
    }

    public bool FillSplined(LocalRect a, LocalRect b, LocalRect c, LocalRect d,
      float t)
    {
      center = Splines.CatmullRom.ToCHS(a.center, b.center, c.center, d.center,
        centripetal: false).PositionAt(t);
      radii = Vector2.Max(Splines.CatmullRom.ToCHS(a.radii, b.radii, c.radii,
        d.radii, centripetal: false).PositionAt(t).ToVector2(), Vector2.zero);
      return true;
    }

    public LineEnumerator TakeLines(float height,
      Margins? cellMargins = null, VerticalOrigin? verticalOrigin = null)
    {
      var useMargins = cellMargins.UnwrapOr(Margins.zero);
      var useVerticalOrigin = verticalOrigin.UnwrapOr(VerticalOrigin.Top);
      return new LineEnumerator(this, height, useMargins, useVerticalOrigin);
    }

    public struct LineEnumerator {
      private LocalRect _rect;
      private float _height;
      private VerticalOrigin _verticalOrigin;
      private Margins _margins;
      private int _index;
      
      public LineEnumerator(LocalRect rect, float height, Margins margins,
        VerticalOrigin verticalOrigin) {
        _rect = rect;
        _height = height;
        _margins = margins;
        _verticalOrigin = verticalOrigin;
        _index = -1;
      }

      public LeapGrid.Cell Current {
        get { return new LeapGrid.Cell() {
          row = _index, col = 0, index = _index, margins = _margins,
          outerRect = 
            (_verticalOrigin == VerticalOrigin.Bottom ?
              new LocalRect(
                top: _index * _height,
                bottom: _index * _height + _height,
                left: _rect.left,
                right: _rect.right
              )
              : new LocalRect(
                top: _rect.top - (_index * _height),
                bottom: _rect.top - (_index * _height + _height),
                left: _rect.left,
                right: _rect.right
              )
            )
        }; }
      }
      public bool MoveNext() {
        _index += 1;
        return _index * _height + _height <= _rect.height;
      }
      public LineEnumerator GetEnumerator() { return this; }
    }

  }

}
