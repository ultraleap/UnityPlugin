/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using Leap.Unity.Query;
using System.Collections.Generic;
using Leap.Unity.Infix;

namespace Leap.Unity.Geometry {

  [System.Serializable]
  public struct Plane {

    public Vector3 center;
    public Direction3 normal;
    public Transform transform;

    private Vector3? _cachedCenter;
    private Direction3? _cachedNormal;
    private Matrix4x4? _cachedMatrix;
    private Matrix4x4? _cachedTransformMatrix;
    /// <summary>
    /// The local to world matrix for this plane. The plane is always locally an
    /// XY plane, with Z being height in front of or behind the plane.
    ///
    /// This matrix is defined by the plane's center and normal, so it's not
    /// directly settable.
    /// </summary>
    public Matrix4x4 matrix {
      get {
        if (!checkIsCacheValid()) { rebuildCache(); }
        return _cachedMatrix.Value;
      }
    }
    private Pose? _cachedPose;
    /// <summary>
    /// The local to world pose for this plane. See Plane.matrix.
    /// </summary>
    public Pose pose {
      get {
        if (!checkIsCacheValid()) { rebuildCache(); }
        return _cachedPose.Value;
      }
    }

    private bool checkIsCacheValid() {
      return _cachedPose.HasValue &&
             _cachedCenter.HasValue &&
             _cachedNormal.HasValue &&
             center == _cachedCenter.Value &&
             Direction3.PointsInSameDirection(normal, _cachedNormal.Value) &&
             ((transform == null && !_cachedTransformMatrix.HasValue) ||
              (_cachedTransformMatrix.HasValue &&
               _cachedTransformMatrix.Value == transform.localToWorldMatrix));
    }
    private void rebuildCache() {
      _cachedMatrix = Matrix4x4.TRS(
        center,
        Quaternion.LookRotation(normal),
        Vector3.one);
      _cachedPose = new Pose(center, Quaternion.LookRotation(normal));

      _cachedCenter = center;
      _cachedNormal = normal;

      if (transform == null) {
        _cachedTransformMatrix = null;
      }
      else {
        _cachedMatrix = transform.localToWorldMatrix * _cachedMatrix;
        _cachedPose = transform.localToWorldMatrix.GetPose() * _cachedPose;
        _cachedTransformMatrix = transform.localToWorldMatrix;
      }

    }

    public Plane(Vector3 center, Direction3 normal) {
      _cachedCenter = null;
      _cachedNormal = null;
      _cachedMatrix = null;
      _cachedPose = null;
      _cachedTransformMatrix = null;
      this.center = center;
      this.normal = normal;
      this.transform = null;
    }
    public Plane(Vector3 center, Direction3 normal, Transform transform) {
      _cachedCenter = null;
      _cachedNormal = null;
      _cachedMatrix = null;
      _cachedPose = null;
      _cachedTransformMatrix = null;
      this.center = center;
      this.normal = normal;
      this.transform = transform;
    }

    private static List<Collider> s_backingUnityBodyCollidersCache;
    private static List<Collider> s_unityBodyCollidersCache {
      get {
        if (s_backingUnityBodyCollidersCache == null) {
          s_backingUnityBodyCollidersCache = new List<Collider>();
        }
        return s_backingUnityBodyCollidersCache;
      }
    }
    public bool CollidesWith(UnityEngine.Rigidbody unityBody,
                             bool includeBehindPlane = false) {
      s_unityBodyCollidersCache.Clear();
      Leap.Unity.Utils.FindOwnedChildComponents<Collider, Rigidbody>(
        unityBody,
        s_unityBodyCollidersCache,
        includeInactiveObjects: false
      );
      foreach (var collider in s_unityBodyCollidersCache) {
        if (this.CollidesWith(collider, includeBehindPlane)) {
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Only guaranteed to be correct for convex colliders.
    /// </summary>
    public bool CollidesWith(UnityEngine.Collider unityCollider,
                             bool includeBehindPlane = false) {
      var planePose = this.pose;
      var planePoseInverse = this.pose.inverse;

      var colliderCenter = getUnityColliderWorldCenter(unityCollider);
      var colliderCenter_plane =
        (planePoseInverse * colliderCenter).position;
      var colliderCenterOnPlane_plane = colliderCenter_plane.WithZ(0f);
      var colliderCenterOnPlane =
        (planePose * colliderCenterOnPlane_plane).position;

      var closestPoint = unityCollider.ClosestPoint(colliderCenterOnPlane);
      var closestPoint_plane = (planePoseInverse * closestPoint).position;

      // Concave shapes will break here; projection from their center is not
      // valid. To support concavity, you'd want to project to the plane from
      // the planeward-most point on the concave collider.
      var planeToClosestPoint_plane =
        closestPoint_plane - colliderCenterOnPlane_plane;

      if (includeBehindPlane) {
        // Plane faces forward on Z.
        return planeToClosestPoint_plane.z <= 0f;
      }
      else {
        return planeToClosestPoint_plane.z == 0f;
      }
    }

    private Vector3 getUnityColliderWorldCenter(
                      UnityEngine.Collider unityCollider) {
      SphereCollider sphere = unityCollider as SphereCollider;
      if (sphere != null) {
        return unityCollider.transform.localToWorldMatrix.MultiplyPoint3x4(
          sphere.center
        );
      }

      CapsuleCollider capsule = unityCollider as CapsuleCollider;
      if (capsule != null) {
        Vector3 a, b;
        capsule.GetCapsulePoints(out a, out b);
        return unityCollider.transform.localToWorldMatrix.MultiplyPoint3x4(
          (a + b) / 2f
        );
      }

      BoxCollider box = unityCollider as BoxCollider;
      if (box != null) {
        return unityCollider.transform.localToWorldMatrix.MultiplyPoint3x4(
          box.center
        );
      }

      MeshCollider mesh = unityCollider as MeshCollider;
      if (mesh != null) {
        // Warning: Only valid for convex meshes.
        return unityCollider.transform.localToWorldMatrix.MultiplyPoint3x4(
          mesh.bounds.center
        );
      }

      throw new System.Exception("(Plane Collision) Collider type not supported: " +
        unityCollider.GetType().ToString());
    }
  }

}
