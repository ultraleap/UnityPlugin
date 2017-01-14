using UnityEngine;

public static class Constraints {

  public static void ConstrainToPoint(this Transform transform, Vector3 oldPoint, Vector3 newPoint, float weight = 1f) {
    Vector3 oldDisplacement = transform.position - oldPoint;
    Vector3 newCenterPosition = newPoint + (transform.position - newPoint).normalized * oldDisplacement.magnitude;
    Vector3 newDisplacement = newCenterPosition - newPoint;

    transform.position = Vector3.Lerp(transform.position, newCenterPosition, weight);
    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.FromToRotation(oldDisplacement, newDisplacement) * transform.rotation, weight);
  }

  public static Vector3 ConstrainToCone(this Vector3 point, Vector3 origin, Vector3 normalDirection, float minDot) {
    return (point - origin).ConstrainToNormal(normalDirection, minDot) + origin;
  }

  public static Vector3 ConstrainToNormal(this Vector3 direction, Vector3 normalDirection, float minDot) {
    if (minDot >= 1f) return normalDirection * direction.magnitude; if (minDot <= -1f) return direction;
    float dot = Mathf.Acos(Mathf.Clamp(Vector3.Dot(direction.normalized, normalDirection.normalized), -1f, 1f));
    return Vector3.Slerp(direction.normalized, normalDirection.normalized, (dot - Mathf.Acos(minDot)) / dot) * direction.magnitude;
  }

  public static Vector3 ConstrainDistance(this Vector3 position, Vector3 anchor, float distance) {
    return anchor + ((position - anchor).normalized * distance);
  }

}