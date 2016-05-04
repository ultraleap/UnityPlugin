using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.DetectionUtilities{

public class Detector : MonoBehaviour {
    public float Period = .1f; //seconds
    public UnityEvent OnDetection;

    #if UNITY_EDITOR
    /* Visual debugging utilities */

    public void DrawCircle(Vector3 center, 
                           Vector3 normal, 
                           float radius, 
                           Color color, 
                           int quality = 32, 
                           float duration = 0, 
                           bool depthTest = true) {
      Vector3 planeA = Vector3.Slerp(normal, -normal, 0.5f);
      Vector3 planeB = Vector3.Cross(normal, planeA).normalized;
      DrawArc(360, center, planeA, normal, radius, color, quality, duration, depthTest);
    }

    /* Adapted from: Zarrax (http://math.stackexchange.com/users/3035/zarrax), Parametric Equation of a Circle in 3D Space?, 
     * URL (version: 2014-09-09): http://math.stackexchange.com/q/73242 */
    public void DrawArc(   float arc,
                           Vector3 center, 
                           Vector3 forward,
                           Vector3 normal,
                           float radius, 
                           Color color, 
                           int quality = 32, 
                           float duration = 0, 
                           bool depthTest = true) {

      Vector3 right = Vector3.Cross(normal, forward).normalized;
      float deltaAngle = arc/quality;
      Vector3 thisPoint = center + forward * radius;
      Vector3 nextPoint = new Vector3();
      for(float angle = 0; Mathf.Abs(angle) <= Mathf.Abs(arc); angle += deltaAngle){
        float cosAngle = Mathf.Cos(angle * Constants.DEG_TO_RAD);
        float sinAngle = Mathf.Sin(angle * Constants.DEG_TO_RAD);
        nextPoint.x = center.x + radius * (cosAngle * forward.x + sinAngle * right.x);
        nextPoint.y = center.y + radius * (cosAngle * forward.y + sinAngle * right.y);
        nextPoint.z = center.z + radius * (cosAngle * forward.z + sinAngle * right.z);
        Debug.DrawLine(thisPoint, nextPoint, color, duration, depthTest);
        thisPoint = nextPoint;
      }
    }

    public void DrawCone(Vector3 origin, 
                           Vector3 direction, 
                           float angle,
                           float height,
                           Color color, 
                           int quality = 4, 
                           float duration = 0, 
                           bool depthTest = true){

      float step = height/quality;
      for(float q = step; q <= height; q += step){
        DrawCircle(origin + direction * q, direction, Mathf.Tan(angle * Constants.DEG_TO_RAD) * q, color, quality * 8, duration, depthTest);
      }
      float hypo = height/Mathf.Cos(angle/2);
      Vector3 edge = Vector3.Slerp(direction, -direction, Mathf.Cos(angle/2)) * hypo;
      Debug.DrawLine(origin, edge);
    }

    #endif

  }

  public enum Directions { Up, Down, Right, Left, Forward, Backward, CustomVector }
}
