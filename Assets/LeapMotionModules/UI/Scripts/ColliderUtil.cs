using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class ColliderUtil {

  private static List<Collider> _colliderList = new List<Collider>();

  //*************
  //GENRAL HELPER

  /* Returns whether or not a given vector is within a given radius to a center point
   */
  public static bool withinDistance(Vector3 point, Vector3 center, float radius) {
    return (point - center).sqrMagnitude < radius * radius;
  }

  public static bool withinDistance(Vector3 pointToOrigin, float radius) {
    return pointToOrigin.sqrMagnitude < radius * radius;
  }

  /* Given a point and a center, return a new point that is along the axis created by the 
   * two vectors, and is a given distance from the center
   */
  public static Vector3 getPointAtDistance(Vector3 point, Vector3 center, float distance) {
    return (point - center).normalized * distance + center;
  }


  /* Given a collider and a position relative to the colliders transform, return whether or not the 
   * position lies within the collider.  Can also provide the allowed distance tolerance
   */
  public static bool isInsideCollider(this Collider collider, Vector3 localPosition, float tolerance = 0.0f) {
    SphereCollider sphereCollider = collider as SphereCollider;
    if (sphereCollider != null) {
      return sphereCollider.isPointInside(localPosition, tolerance);
    }
    BoxCollider boxCollider = collider as BoxCollider;
    if (boxCollider != null) {
      return boxCollider.isPointInside(localPosition, tolerance);
    }
    CapsuleCollider capsuleCollider = collider as CapsuleCollider;
    if (capsuleCollider != null) {
      return capsuleCollider.isPointInside(localPosition, tolerance);
    }
    Debug.LogWarning("ColliderUtil does not work for Mesh Colliders!");
    return false;
  }

  /* Given a collider and a position reltative to the colliders transform, return the point on the
   * surface of the collider closest to the position
   */
  public static Vector3 closestPointOnSurface(this Collider collider, Vector3 localPosition, float extrude = 0.0f) {
    SphereCollider sphereCollider = collider as SphereCollider;
    if (sphereCollider != null) {
      return sphereCollider.closestPointOnSurface(localPosition, extrude);
    }
    BoxCollider boxCollider = collider as BoxCollider;
    if (boxCollider != null) {
      return boxCollider.closestPointOnSurface(localPosition, extrude);
    }
    CapsuleCollider capsuleCollider = collider as CapsuleCollider;
    if (capsuleCollider != null) {
      return capsuleCollider.closestPointOnSurface(localPosition, extrude);
    }
    Debug.LogWarning("ColliderUtil does not work for Mesh Colliders!");
    return Vector3.zero;
  }

  /* Given a GameObject and a position in global space, return whether or not the position lies within
   * one of the colliders on the GameObject or one of it's children.  Can also provide the allowed 
   * distance tolerance
   */
  public static bool isInsideColliders(GameObject obj, Vector3 globalPosition, float tolerance = 0.0f) {
    obj.GetComponentsInChildren<Collider>(false, _colliderList);
    foreach (Collider collider in _colliderList) {
      Vector3 localPosition = collider.transform.InverseTransformPoint(globalPosition);
      if (collider.isInsideCollider(localPosition, tolerance)) {
        return true;
      }
    }
    return false;
  }

  /* Given a GameObject and a position in global space, return a point on the surface of one of the
   * colliders on the GameObject or one of it's children that is closest to the given position
   */
  public static Vector3 closestPointOnSurfaces(GameObject obj, Vector3 globalPosition, float extrude = 0.0f) {
    obj.GetComponentsInChildren<Collider>(false, _colliderList);

    Transform chosenTransform = null;
    Vector3 closestPoint = Vector3.zero;
    float closestDistance = float.MaxValue;

    foreach (Collider collider in _colliderList) {
      Vector3 localPoint = collider.transform.InverseTransformPoint(globalPosition);
      Vector3 point = collider.closestPointOnSurface(localPoint, extrude);
      float distance = (globalPosition - point).sqrMagnitude;
      if (distance < closestDistance) {
        chosenTransform = collider.transform;
        closestDistance = distance;
        closestPoint = point;
      }
    }

    return chosenTransform.TransformPoint(closestPoint);
  }

  //***************
  //Raycasting

  public static bool segmentCast(GameObject obj, Vector3 start, Vector3 end, out RaycastHit hitInfo) {
    Ray ray = new Ray(start, end - start);
    float dist = Vector3.Distance(start, end);

    obj.GetComponentsInChildren<Collider>(false, _colliderList);

    hitInfo = new RaycastHit();

    bool hitAny = false;
    RaycastHit tempHit;
    foreach (Collider collider in _colliderList) {
      if (collider.Raycast(ray, out tempHit, dist)) {
        if (!hitAny || tempHit.distance < hitInfo.distance) {
          hitInfo = tempHit;
          hitAny = true;
        }
      }
    }

    return hitAny;
  }

  //***************
  //SPHERE COLLIDER

  public static bool isPointInside(this SphereCollider collider, Vector3 localPosition, float tolerance = 0.0f) {
    localPosition -= collider.center;
    return withinDistance(localPosition, collider.radius + tolerance);
  }

  public static Vector3 closestPointOnSurface(this SphereCollider collider, Vector3 localPosition, float extrude = 0.0f) {
    return getPointAtDistance(localPosition, collider.center, collider.radius + extrude);
  }

  //************
  //BOX COLLIDER

  public static bool isPointInside(this BoxCollider collider, Vector3 localPosition, float tolerance = 0.0f) {
    localPosition -= collider.center;
    if (Mathf.Abs(localPosition.x) > collider.size.x / 2.0f + tolerance) {
      return false;
    }
    if (Mathf.Abs(localPosition.y) > collider.size.y / 2.0f + tolerance) {
      return false;
    }
    if (Mathf.Abs(localPosition.z) > collider.size.z / 2.0f + tolerance) {
      return false;
    }
    return true;
  }

  public static Vector3 closestPointOnSurface(this BoxCollider collider, Vector3 localPosition, float extrude = 0.0f) {
    localPosition -= collider.center;

    Vector3 radius = collider.size / 2.0f;
    radius.x += extrude;
    radius.y += extrude;
    radius.z += extrude;

    localPosition.x = Mathf.Clamp(localPosition.x, -radius.x, radius.x);
    localPosition.y = Mathf.Clamp(localPosition.y, -radius.y, radius.y);
    localPosition.z = Mathf.Clamp(localPosition.z, -radius.z, radius.z);

    //If an internal point
    if (Mathf.Abs(localPosition.x) < radius.x &&
        Mathf.Abs(localPosition.y) < radius.y &&
        Mathf.Abs(localPosition.z) < radius.z) {
      //Snap closest axis to the bounds
      //Farthest from the center
      if (Mathf.Abs(localPosition.x) > Mathf.Abs(localPosition.y)) {
        if (Mathf.Abs(localPosition.x) > Mathf.Abs(localPosition.z)) {
          //x is farthest
          localPosition.x = (localPosition.x > 0) ? radius.x : -radius.x;
        }
        else {
          //z is farthest
          localPosition.z = (localPosition.z > 0) ? radius.z : -radius.z;
        }
      }
      else {
        if (Mathf.Abs(localPosition.y) > Mathf.Abs(localPosition.z)) {
          //y is farthest
          localPosition.y = (localPosition.y > 0) ? radius.y : -radius.y;
        }
        else {
          //z is farthest
          localPosition.z = (localPosition.z > 0) ? radius.z : -radius.z;
        }
      }
    }

    localPosition += collider.center;
    return localPosition;
  }

  //****************
  //CAPSULE COLLIDER

  /* A capsule is defined as a line segment with a radius.  This method returns the two endpoints of that segment, 
   * as well as it's length, in local space.
   */
  public static void getSegmentInfo(this CapsuleCollider collider, out Vector3 localV0, out Vector3 localV1, out float length) {
    Vector3 axis = Vector3.right;
    if (collider.direction == 1) {
      axis = Vector3.up;
    }
    else {
      axis = Vector3.forward;
    }

    length = Mathf.Max(0, collider.height - collider.radius * 2);
    localV0 = axis * length / 2.0f + collider.center;
    localV1 = -axis * length / 2.0f + collider.center;
  }

  public static bool isPointInside(this CapsuleCollider collider, Vector3 localPosition, float tolerance = 0.0f) {
    Vector3 v0, v1;
    float length;
    collider.getSegmentInfo(out v0, out v1, out length);

    if (length == 0.0f) {
      return withinDistance(localPosition, collider.radius + tolerance);
    }

    float t = Vector3.Dot(localPosition - v0, v1 - v0) / (length * length);
    if (t <= 0.0f) {
      return withinDistance(localPosition, v0, collider.radius + tolerance);
    }
    if (t >= 1.0f) {
      return withinDistance(localPosition, v1, collider.radius + tolerance);
    }

    Vector3 projection = v0 + t * (v1 - v0);
    return withinDistance(localPosition, projection, collider.radius + tolerance);
  }

  public static Vector3 closestPointOnSurface(this CapsuleCollider collider, Vector3 localPosition, float extrude = 0.0f) {
    Vector3 v0, v1;
    float length;
    collider.getSegmentInfo(out v0, out v1, out length);

    if (length == 0.0f) {
      return getPointAtDistance(localPosition, collider.center, collider.radius);
    }

    float t = Vector3.Dot(localPosition - v0, v1 - v0) / (length * length);
    if (t <= 0.0f) {
      return getPointAtDistance(localPosition, v0, collider.radius + extrude);
    }
    if (t >= 1.0f) {
      return getPointAtDistance(localPosition, v1, collider.radius + extrude);
    }

    Vector3 projection = v0 + t * (v1 - v0);
    return getPointAtDistance(localPosition, projection, collider.radius + extrude);
  }

  public static float distanceToSegment(Vector3 a, Vector3 b, Vector3 p) {
    float t = Vector3.Dot(p - a, b - a) / Vector3.Dot(a - b, a - b);
    if (t <= 0.0f) {
      return Vector3.Distance(p, a);
    }
    if (t >= 1.0f) {
      return Vector3.Distance(p, b);
    }
    Vector3 projection = a + t * (b - a);
    return Vector3.Distance(p, projection);
  }

}