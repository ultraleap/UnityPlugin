/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Infix;
using Leap.Unity.RuntimeGizmos;
using UnityEngine;

// using Drawer = Leap.Unity.RuntimeGizmos.RuntimeGizmoDrawer;

namespace Leap.Unity.Geometry {

  public static class GeometryUtils {

    /// <summary> Converts a sample point from the unit square (XY [-1, 1]) to
    /// a point in the unit circle.
    /// See: https://www.shadertoy.com/view/3dsSWs </summary>
    public static Vector2 Square2Circle(this Vector2 unitSquarePoint) {
      var v = unitSquarePoint;
      return v.Abs().CompMax() *
        (v.CompMul(Vector2.one * 2.0f + v.Abs())).normalized;
    }

    #region Runtime Gizmos

    private static Drawer s_drawer;

    #region Render Sphere

    /// <summary>
    /// Renders this Sphere in world space.
    /// </summary>
    public static void Render(this Sphere sphere) {
      Drawer drawer;
      if (TryGetDrawer(out drawer)) {
        drawer.Sphere(sphere.center, sphere.radius);
      }
    }

    public static void Render(this Sphere sphere, Color color) {
      Drawer drawer;
      if (TryGetDrawer(out drawer)) {
        drawer.color = color;
        drawer.Sphere(sphere.center, sphere.radius);
      }
    }

    #endregion

    #region Render Point

    /// <summary>
    /// Renders the point with world-aligned axis lines one centimeter outward from the
    /// point's position. Optionally pass a scale multliplier for the scale of the
    /// render.
    /// </summary>
    public static void Render(this Point point, Color? color, float scale = 1f) {
      Drawer drawer;
      if (TryGetDrawer(out drawer)) {

        float finalScale = 0.01f * scale;
        var pos = point.position;

        if (color.HasValue) {
          drawer.color = color.GetValueOrDefault();
        }

        if (!color.HasValue) {
          drawer.color = Color.red;
        }
        drawer.Line(pos - Vector3.right * finalScale,
                    pos + Vector3.right * finalScale);

        if (!color.HasValue) {
          drawer.color = Color.green;
        }
        drawer.Line(pos - Vector3.up * finalScale,
                    pos + Vector3.up * finalScale);

        if (!color.HasValue) {
          drawer.color = Color.blue;
        }
        drawer.Line(pos - Vector3.forward * finalScale,
                    pos + Vector3.forward * finalScale);
      }
    }
    /// <summary>
    /// Renders the point with a sphere of one centimeter radius. Optionally pass a
    /// color and scale multiplier.
    /// </summary>
    public static void RenderSphere(this Point point, Color? color, float scale = 1f) {
      Drawer drawer;
      if (TryGetDrawer(out drawer)) {

        float finalScale = 0.01f * scale;
        var pos = point.position;

        if (color.HasValue) {
          drawer.color = color.GetValueOrDefault();
        }

        drawer.Sphere(pos, finalScale);
      }
    }

    #endregion

    #endregion

    #region Internal

    private static bool TryGetDrawer(out Drawer drawer) {
      drawer = Drawer.UnityDebugDrawer;
      return true;
    }

    #endregion

  }

}
