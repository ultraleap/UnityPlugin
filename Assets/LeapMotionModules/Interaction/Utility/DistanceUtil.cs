using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public static class DistanceUtil {

    #region Comparative Squared Center Distance

    /// <summary>
    /// Returns the squared distance to the center of the collider. If the collider is an oblong shape -- a capsule or a
    /// box collider - returns the squared distance to the line segment at the center of the capsule or box.
    /// 
    /// Values returned by this function are suitable for proximity comparisons amongst other values returned by this function,
    /// and require fewer calculations than full distance-field calculations.
    /// </summary>
    public static float GetComparativeSqrCenterDistance(Collider collider, Vector3 worldPosition) {
      float distance;

      if (collider is SphereCollider) {
        distance = GetComparativeCenterSqrDistance((SphereCollider)collider, worldPosition);
      }
      else if (collider is CapsuleCollider) {
        distance = GetComparativeCenterSqrDistance((CapsuleCollider)collider, worldPosition);
      }
      else if (collider is BoxCollider) {
        distance = GetComparativeCenterSqrDistance((BoxCollider)collider, worldPosition);
      }
      else if (collider is MeshCollider) {
        distance = GetComparativeCenterSqrDistance((MeshCollider)collider, worldPosition);
      }
      else {
        distance = (collider.transform.position - worldPosition).sqrMagnitude;
      }

      return distance;
    }

    /// <summary>
    /// Returns the squared distance to the center of the argument SphereCollider.
    /// </summary>
    public static float GetComparativeCenterSqrDistance(SphereCollider sphere, Vector3 worldPosition) {
      return (sphere.transform.TransformPoint(sphere.center) - worldPosition).sqrMagnitude;
    }

    /// <summary>
    /// Returns the squared distance to the line segment at the center of the CapsuleCollider.
    /// </summary>
    public static float GetComparativeCenterSqrDistance(CapsuleCollider capsule, Vector3 worldPosition) {
      Vector3 dir = GetCapsuleDir(capsule);

      Vector3 a = capsule.center + dir * (capsule.height / 2F - capsule.radius);
      Vector3 b = capsule.center - dir * (capsule.height / 2F - capsule.radius);
      return (worldPosition.ConstrainToSegment(capsule.transform.TransformPoint(a), capsule.transform.TransformPoint(b)) - worldPosition)
               .sqrMagnitude;
    }

    /// <summary>
    /// Returns a squared distance to the "center" of the BoxCollider, suitable for distance comparisons.
    /// If the BoxCollider is a cube, returns the squared distance to the center point of the cube.
    /// If the BoxCollider is not a cube, returns the squared distance to the line segment at the center
    /// of the box aligned with the longest component of the box.
    /// </summary>
    public static float GetComparativeCenterSqrDistance(BoxCollider box, Vector3 worldPosition) {
      Vector3 size = box.size;
      Vector3 largestCompDir, middleCompDir, smallestCompDir;
      GetComponentSizeOrder(box.size, out largestCompDir, out middleCompDir, out smallestCompDir);
      float largestCompLength = Vector3.Dot(largestCompDir, size);
      float middleCompLength = Vector3.Dot(middleCompDir, size);

      Vector3 dir = largestCompDir;
      Vector3 a = box.center + dir * (largestCompLength / 2F - middleCompLength);
      Vector3 b = box.center - dir * (largestCompLength / 2F - middleCompLength);
      return (worldPosition.ConstrainToSegment(box.transform.TransformPoint(a), box.transform.TransformPoint(b)) - worldPosition)
                .sqrMagnitude;
    }

    /// <summary>
    /// Outputs the largest, middle, and smallest components of the "components" vector as normalized directions;
    /// e.g. use Vector3.Scale(components, smallestCompDir) to access a Vector3 with the other components zeroed out,
    /// or use Vector3.Dot(components, smallestCompDir) to access the float value of the smallest component of the vector.
    /// </summary>
    private static void GetComponentSizeOrder(Vector3 components, out Vector3 largestCompDir, out Vector3 middleCompDir, out Vector3 smallestCompDir) {
      Vector3 c = components;
      Vector3 xDir = Vector3.right, yDir = Vector3.up, zDir = Vector3.forward;
      if (c.x > c.y) {
        if (c.z > c.x) {
          largestCompDir = zDir; middleCompDir = xDir; smallestCompDir = yDir;
        }
        else {
          largestCompDir = xDir;
          if (c.y > c.z) {
            middleCompDir = yDir; smallestCompDir = zDir;
          }
          else {
            middleCompDir = zDir; smallestCompDir = yDir;
          }
        }
      }
      else {
        if (c.z > c.x) {
          smallestCompDir = xDir;
          if (c.y > c.z) {
            largestCompDir = yDir; middleCompDir = zDir;
          }
          else {
            largestCompDir = zDir; middleCompDir = yDir;
          }
        }
        else {
          largestCompDir = yDir; middleCompDir = xDir; smallestCompDir = zDir;
        }
      }
    }

    /// <summary>
    /// Gets a comparative squared distance from the center of the MeshCollider's axis-aligned bounding box.
    /// </summary>
    public static float GetComparativeCenterSqrDistance(MeshCollider mesh, Vector3 worldPosition) {
      return (mesh.transform.TransformPoint(mesh.bounds.center) - worldPosition).sqrMagnitude;
    }

    #endregion

    #region Surface Distance Fields

    /// <summary>
    /// Returns the squared distance to the surface of the argument collider. Does NOT return an accurate result for
    /// MeshColliders; only returns the distance to the surface of the MeshCollider's axis-aligned bounding box.
    /// </summary>
    public static float GetSqrDistanceToSurface(Collider collider, Vector3 worldPosition) {
      float distance;

      if (collider is SphereCollider) {
        distance = GetApproxSqrDistanceToSurface((SphereCollider)collider, worldPosition);
      }
      else if (collider is CapsuleCollider) {
        distance = GetApproxSqrDistanceToSurface((CapsuleCollider)collider, worldPosition);
      }
      else if (collider is BoxCollider) {
        distance = GetSqrDistanceToSurface((BoxCollider)collider, worldPosition);
      }
      else if (collider is MeshCollider) {
        distance = GetSqrDistanceToSurface((MeshCollider)collider, worldPosition);
      }
      else {
        distance = (collider.transform.position - worldPosition).sqrMagnitude;
      }

      return distance;
    }

    /// <summary>
    /// Returns an approximate distance to the surface of the sphere collider; no square roots.
    /// Won't work correctly for non-uniformly-scaled spheres; will default to using the largest scale
    /// factor for the sphere radius.
    /// </summary>
    public static float GetApproxSqrDistanceToSurface(SphereCollider collider, Vector3 worldPosition) {
      Vector3 worldSpherePos = collider.transform.TransformPoint(collider.center);
      float worldRadius = collider.radius * collider.transform.lossyScale.LargestComp();
      Vector3 approxPosOnSphere = GetApproxPointOnSphere(worldSpherePos, worldRadius * worldRadius, worldPosition);

      return (approxPosOnSphere - worldPosition).sqrMagnitude;
    }

    public static Vector3 GetApproxPointOnSphere(Vector3 spherePos, float sqrRadius, Vector3 position) {
      return position.ApproxConstrainToSphere(spherePos, sqrRadius)
                     .ApproxConstrainToSphere(spherePos, sqrRadius);
    }

    private static Vector3 ApproxConstrainToSphere(this Vector3 position, Vector3 spherePos, float sqrRadius) {
      Vector3 offset = (position - spherePos);
      offset *= (sqrRadius / (offset.sqrMagnitude + sqrRadius) - 0.5F) * 2F;
      return position + offset;
    }

    /// <summary>
    /// Returns an approximate squared distance to the surface of the capsule collider; no square roots.
    /// (Also works for non-uniformly-scaled capsules.)
    /// </summary>
    public static float GetApproxSqrDistanceToSurface(CapsuleCollider collider, Vector3 worldPosition) {
      Vector3 capsulePos = collider.transform.TransformPoint(collider.center);
      Vector3 capsuleTip = collider.transform.TransformPoint(collider.center + GetCapsuleDir(collider) * (collider.height / 2F - collider.radius));

      Vector3 effectiveSpherePos = worldPosition.ConstrainToSegment(capsuleTip, capsulePos + capsulePos - capsuleTip);
      float effectiveSqrRadius = collider.direction == 0 ?   // direction is X axis
                                   collider.transform.localScale.y > collider.transform.localScale.z ?
                                     collider.transform.TransformVector(collider.radius * Vector3.up).sqrMagnitude
                                   : collider.transform.TransformVector(collider.radius * Vector3.forward).sqrMagnitude
                                 : collider.direction == 1 ? // direction is Y axis
                                   collider.transform.localScale.x > collider.transform.localScale.z ?
                                     collider.transform.TransformVector(collider.radius * Vector3.right).sqrMagnitude
                                   : collider.transform.TransformVector(collider.radius * Vector3.forward).sqrMagnitude
                                 :                           // direction is Z axis
                                   collider.transform.localScale.x > collider.transform.localScale.y ?
                                     collider.transform.TransformVector(collider.radius * Vector3.right).sqrMagnitude
                                   : collider.transform.TransformVector(collider.radius * Vector3.up).sqrMagnitude;

      Vector3 approxPosOnSphere = GetApproxPointOnSphere(effectiveSpherePos, effectiveSqrRadius, worldPosition);
      return (approxPosOnSphere - worldPosition).sqrMagnitude;
    }

    /// <summary>
    /// Returns the squared distance from worldPosition to the surface of the BoxCollider.
    /// </summary>
    public static float GetSqrDistanceToSurface(BoxCollider collider, Vector3 worldPosition) {
      Vector3 boxOrigin = collider.transform.TransformPoint(collider.center);

      Quaternion axisAligningRotation = Quaternion.Inverse(collider.transform.rotation);
      Vector3 boxExtentsAligned = axisAligningRotation * collider.transform.TransformVector(collider.size / 2F);
      Vector3 pointAligned      = axisAligningRotation * (worldPosition - boxOrigin);

      Vector3 closestPointOnBox = GetClosestPointOnBox(boxExtentsAligned, pointAligned);

      // actualPosOnBox = boxOrigin + collider.transform.rotation * closestPointOnBox;

      return (pointAligned - closestPointOnBox).sqrMagnitude;
    }

    private static Vector3 GetClosestPointOnBox(Vector3 boxExtents, Vector3 point) {
      Vector3 b = boxExtents;
      Vector3 p = point;
      int xSign = Sign(p.x), ySign = Sign(p.y), zSign = Sign(p.z); // store signs for later
      p = p.Abs(); // remove sign information for extent clamping
      Vector3 d = p - b;

      if (d.x > 0 || d.y > 0 || d.z > 0) { // outside the box
        // Set all components beyond the corresponding box extent component
        // to the box's extent component.
        for (int i = 0; i < 3; i++) {
          if (d[i] > 0) {
            p[i] = b[i];
          }
        }
      }
      else { // inside the box -- d.x, d.y, d.z all negative or zero
        // Set the component of P for which D's component is largest (least negative),
        // in other words, closest to the box wall, to be the corresponding component of B,
        // which will move it to that box wall.
        int dLargestCompIdx = d.LargestCompIdx();
        p[dLargestCompIdx] = b[dLargestCompIdx];
      }

      // Restore sign information and return.
      return new Vector3(p.x * xSign, p.y * ySign, p.z * zSign);
    }

    private static int Sign(float f) {
      return f == 0 ? 0 : f > 0 ? 1 : -1;
    }

    /// <summary>
    /// Returns the squared distance from worldPosition to the surface of the MeshCollider's bounds.
    /// </summary>
    public static float GetSqrDistanceToSurface(MeshCollider collider, Vector3 worldPosition) {
      Vector3 boxOrigin = collider.transform.TransformPoint(collider.bounds.center);

      return GetSqrDistanceToBox(collider.bounds.extents, worldPosition - boxOrigin);
    }

    private static float GetSqrDistanceToBox(Vector3 box, Vector3 pos) {
      Vector3 delta = (pos - box);
      float insideDist = Mathf.Min(Mathf.Max(delta.x, Mathf.Max(delta.y, delta.z)), 0F); // zero if the position is actually outside the box
      return insideDist * insideDist + (Vector3.Max(delta, Vector3.zero)).sqrMagnitude;
    }

    #endregion

    #region Common

    private static Vector3 GetCapsuleDir(CapsuleCollider capsule) {
      // https://docs.unity3d.com/ScriptReference/CapsuleCollider-direction.html
      return capsule.direction == 0 ? Vector3.right : capsule.direction == 1 ? Vector3.up : Vector3.forward;
    }

    #endregion

  }

}