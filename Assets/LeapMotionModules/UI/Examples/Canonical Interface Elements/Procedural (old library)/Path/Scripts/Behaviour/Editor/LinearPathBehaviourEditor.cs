using UnityEngine;
using UnityEditor;

namespace Procedural.DynamicPath {

  [CustomEditor(typeof(LinearPathBehaviour))]
  public class LinearPathBehaviourEditor : Editor {

    void OnSceneGUI() {
      var linearPath = target as LinearPathBehaviour;

      var path = linearPath.Path;
      float length = path.Length;
      Vector3 end = path.GetPosition(length);

      EditorGUI.BeginChangeCheck();

      float size = 0.1f * HandleUtility.GetHandleSize(end);

      Vector3 pos = Handles.Slider(end, linearPath.transform.forward, size, Handles.DotCap, 0);

      if (EditorGUI.EndChangeCheck()) {
        pos = closestPointOnRay(linearPath.transform.position, linearPath.transform.forward);

        if (Vector3.Dot(pos - linearPath.transform.position, linearPath.transform.forward) > 0) {
          Undo.RecordObject(linearPath, "Moved path");

          linearPath.Length = (pos - linearPath.transform.position).magnitude;

          EditorUtility.SetDirty(linearPath);
          serializedObject.Update();
        }
      }
    }

    public static Vector3 closestPointOnRay(Vector3 origin, Vector3 direction) {
      Vector2 mouse = Event.current.mousePosition;
      Ray ray = HandleUtility.GUIPointToWorldRay(mouse);

      Vector3 c1, c2;
      ClosestPointsOnTwoLines(out c1, out c2, ray.origin, ray.direction, origin, direction);

      return c1;
    }

    public static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1,
                                               out Vector3 closestPointLine2,
                                               Vector3 linePoint1,
                                               Vector3 lineVec1,
                                               Vector3 linePoint2,
                                               Vector3 lineVec2) {

      closestPointLine1 = Vector3.zero;
      closestPointLine2 = Vector3.zero;

      float a = Vector3.Dot(lineVec1, lineVec1);
      float b = Vector3.Dot(lineVec1, lineVec2);
      float e = Vector3.Dot(lineVec2, lineVec2);

      float d = a * e - b * b;

      //lines are not parallel
      if (d != 0.0f) {

        Vector3 r = linePoint1 - linePoint2;
        float c = Vector3.Dot(lineVec1, r);
        float f = Vector3.Dot(lineVec2, r);

        float s = (b * f - c * e) / d;
        float t = (a * f - c * b) / d;

        closestPointLine1 = linePoint1 + lineVec1 * s;
        closestPointLine2 = linePoint2 + lineVec2 * t;

        return true;
      } else {
        return false;
      }
    }
  }
}
