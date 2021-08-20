/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Geometry {

  public struct Margins {

    public float left;
    public float right;
    public float top;
    public float bottom;

    public static readonly Margins zero = Margins.All(0f);
    public static readonly Margins one = Margins.All(1f);

    public Margins(float left, float right, float top, float bottom) {
      this.left = left; this.right = right; this.top = top; this.bottom = bottom;
    }

    public static Margins All(float margin) {
      return new Margins() {
        left = margin, right = margin, top = margin, bottom = margin
      };
    }

    public static Margins operator *(Margins m, float factor) {
      return new Margins(m.left * factor, m.right * factor, m.top * factor,
        m.bottom * factor);
    }

    public static Margins operator *(float factor, Margins m) {
      return new Margins(m.left * factor, m.right * factor, m.top * factor,
        m.bottom * factor);
    }

    public static Margins operator -(Margins m) {
      return m * -1f;
    }

  }

  public static class MarginExtensions {

    public static LocalRect PadOuter(this LocalRect r, Margins margins) {
      return new LocalRect(
        center: r.center + new Vector3(
          (margins.right - margins.left) / 2f,
          (margins.top - margins.bottom) / 2f,
          0f
        ),
        radii: new Vector2(
          r.radii.x + (margins.right + margins.left) / 2f,
          r.radii.y + (margins.top + margins.bottom) / 2f
        )
      );
    }

    public static LocalRect PadInner(this LocalRect r, Margins margins) {
      return r.PadOuter(-margins);
    }

  }

}
