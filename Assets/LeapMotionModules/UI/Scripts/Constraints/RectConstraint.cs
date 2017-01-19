using Leap.Unity;
using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Constraints {

  public class RectConstraint : Linear2Constraint {

    public enum RectAnchor { Top, Middle, Bottom }

    [Header("Rect Configuration")]
    [Tooltip("The local-space normal of the Rect.")]
    public Axis rectAxis = Axis.Z;
    //[Tooltip("The line constraint can be specified relative to this.transform.localPosition as its top, middle, or bottom.")]
    //public RectAnchor rectAnchor = RectAnchor.Middle;
    [MinValue(0.0001F)]
    [Tooltip("The width and height of the RectConstraint. This value is NOT affected by this.transform.localScale.")]
    public Vector2 bounds = new Vector2(0.04F, 0.04F);

    public Vector3 LocalCenter {
      get { return constraintLocalPosition; }
    }

    public override Vector3 EvaluatePositionOnConstraint(Vector2 coordinates) {
      return LocalCenter + (ExpandByAxis(coordinates).Map(0, 1, -1, 1)).CompMul(ExpandByAxis(bounds / 2));
    }

    public Vector2 GetConstraintProjectionCoords(Vector3 localPosition) {
      return ClipByAxis(((localPosition - LocalCenter).CompDiv(ExpandByAxis(bounds / 2))).Map(-1, 1, 0, 1));
    }

    public override Vector2 GetConstraintProjectionCoords() {
      return GetConstraintProjectionCoords(this.transform.localPosition);
    }

    public override void EnforceConstraint() {
      this.transform.localPosition = EvaluatePositionOnConstraint(GetConstraintProjectionCoords());
    }

    private Vector3 ExpandByAxis(Vector2 vec2) {
      switch (rectAxis) {
        case Axis.X:
          return new Vector3(0, vec2.x, vec2.y);
        case Axis.Y:
          return new Vector3(vec2.x, 0, vec2.y);
        case Axis.Z: default:
          return new Vector3(vec2.x, vec2.y, 0);
      }
    }

    private Vector2 ClipByAxis(Vector3 localSpaceVector) {
      switch (rectAxis) {
        case Axis.X:
          return new Vector2(localSpaceVector.y, localSpaceVector.z);
        case Axis.Y:
          return new Vector2(localSpaceVector.x, localSpaceVector.z);
        case Axis.Z: default:
          return new Vector2(localSpaceVector.x, localSpaceVector.y);
      }
    }

    private Vector2 SquareClamp(Vector2 vec, Vector2 squareRadii) {
      float x = Mathf.Clamp(vec.x, -squareRadii.x, squareRadii.x);
      float y = Mathf.Clamp(vec.y, -squareRadii.y, squareRadii.y);
      return new Vector2(x, y);
    }

    #region Gizmos

    private const float GIZMO_THICKNESS = 0.01F;

    void OnDrawGizmos() {
      DrawLinedRect((x) => { return this.transform.parent.TransformPoint(EvaluatePositionOnConstraint(x)); }, 8);
    }

    private void DrawLinedRect(Func<Vector2, Vector3> WorldSpacePointFrom01Coord, int numDivisions = 4) {
      bool colorChanged = false;
      Gizmos.color = DefaultGizmoColor;
      for (int i = 0; i < numDivisions; i++) {
        DrawLineRect(WorldSpacePointFrom01Coord, (float)i / (numDivisions * 2), (float)(numDivisions * 2 - i) / (numDivisions * 2));
        if (!colorChanged) {
          Gizmos.color = DefaultGizmoSubtleColor;
          colorChanged = true;
        }
      }
    }

    private void DrawLineRect(Func<Vector2, Vector3> WorldSpacePointFrom01Coord, float minValue, float maxValue) {
      Gizmos.DrawLine(WorldSpacePointFrom01Coord(new Vector2(minValue, minValue)),
                      WorldSpacePointFrom01Coord(new Vector2(minValue, maxValue)));
      Gizmos.DrawLine(WorldSpacePointFrom01Coord(new Vector2(minValue, maxValue)),
                      WorldSpacePointFrom01Coord(new Vector2(maxValue, maxValue)));
      Gizmos.DrawLine(WorldSpacePointFrom01Coord(new Vector2(maxValue, maxValue)),
                      WorldSpacePointFrom01Coord(new Vector2(maxValue, minValue)));
      Gizmos.DrawLine(WorldSpacePointFrom01Coord(new Vector2(maxValue, minValue)),
                      WorldSpacePointFrom01Coord(new Vector2(minValue, minValue)));
    }

    #endregion

  }

}