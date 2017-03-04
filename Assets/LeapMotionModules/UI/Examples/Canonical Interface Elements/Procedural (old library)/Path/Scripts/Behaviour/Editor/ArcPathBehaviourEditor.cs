using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Procedural.DynamicPath {

  [CustomEditor(typeof(ArcPathBehaviour))]
  public class ArcPathBehaviourEditor : Editor {

    void OnSceneGUI() {
      var arc = target as ArcPathBehaviour;

      IPath path = arc.Path;

      float length = path.Length;

      Vector3 start = arc.transform.TransformPoint(path.GetPosition(0));
      Vector3 mid = arc.transform.TransformPoint(path.GetPosition(length / 2));
      Vector3 end = arc.transform.TransformPoint(path.GetPosition(length));
      Vector3 center = arc.transform.TransformPoint(arc.LocalCenter);

      EditorGUI.BeginChangeCheck();
      float newStartAngle = getAngle(center, start);

      if (EditorGUI.EndChangeCheck()) {
        Undo.RecordObject(arc, "Changed angle");

        while (newStartAngle < arc.EndAngle) {
          newStartAngle += 360;
        }
        while (newStartAngle > arc.EndAngle) {
          newStartAngle -= 360;
        }

        arc.StartAngle = newStartAngle;

        EditorUtility.SetDirty(arc);
        serializedObject.Update();
      }

      EditorGUI.BeginChangeCheck();
      float newEndAngle = getAngle(center, end);

      if (EditorGUI.EndChangeCheck()) {
        Undo.RecordObject(arc, "Changed angle");

        while (newEndAngle > arc.StartAngle) {
          newEndAngle -= 360;
        }
        while (newEndAngle < arc.StartAngle) {
          newEndAngle += 360;
        }

        arc.EndAngle = newEndAngle;

        EditorUtility.SetDirty(arc);
        serializedObject.Update();
      }

      EditorGUI.BeginChangeCheck();
      float size = 0.06f * HandleUtility.GetHandleSize(mid);
      Handles.FreeMoveHandle(mid, Quaternion.identity, size, Vector3.zero, Handles.DotCap);

      if (EditorGUI.EndChangeCheck()) {
        Vector3 constPos = HandleUtility.ClosestPointToPolyLine(arc.transform.position, arc.transform.position + (mid - arc.transform.position) * 2);
        float newRadius = (constPos - arc.transform.position).magnitude;

        if (newRadius > 0) {
          Undo.RecordObject(arc, "Changed radius");

          arc.Radius = newRadius;

          EditorUtility.SetDirty(arc);
          serializedObject.Update();
        }
      }
    }

    private float getAngle(Vector3 center, Vector3 position) {
      float size = 0.06f * HandleUtility.GetHandleSize(position);
      Handles.FreeMoveHandle(position, Quaternion.identity, size, Vector3.zero, Handles.DotCap);

      var arc = target as ArcPathBehaviour;
      Vector3 constrainedPoint = HandleUtility.ClosestPointToArc(center, arc.transform.forward, -arc.transform.right, 360, arc.Radius);

      Vector3 localPosition = arc.transform.InverseTransformPoint(constrainedPoint) - arc.LocalCenter;

      float newAngle = Mathf.Rad2Deg * Mathf.Atan2(localPosition.y, localPosition.x);

      return newAngle;
    }

  }
}
