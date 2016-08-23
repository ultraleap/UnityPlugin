using UnityEngine;
using System.Collections;

namespace Leap.Unity.Interaction {

  public static class PhysicsUtility {

    public static Vector3 ToLinearVelocity(Vector3 deltaPosition, float deltaTime) {
      return deltaPosition / deltaTime;
    }

    public static Vector3 ToLinearVelocity(Vector3 startPosition, Vector3 destinationPosition, float deltaTime) {
      return ToLinearVelocity(destinationPosition - startPosition, deltaTime);
    }

    public static Vector3 ToAngularVelocity(Quaternion deltaRotation, float deltaTime) {
      Vector3 deltaAxis;
      float deltaAngle;
      deltaRotation.ToAngleAxis(out deltaAngle, out deltaAxis);

      if (float.IsInfinity(deltaAxis.x)) {
        deltaAxis = Vector3.zero;
        deltaAngle = 0;
      }

      if (deltaAngle > 180) {
        deltaAngle -= 360.0f;
      }

      return deltaAxis * deltaAngle * Mathf.Deg2Rad / deltaTime;
    }

    public static Vector3 ToAngularVelocity(Quaternion startRotation, Quaternion destinationRotation, float deltaTime) {
      return ToAngularVelocity(destinationRotation * Quaternion.Inverse(startRotation), deltaTime);
    }
  }
}
