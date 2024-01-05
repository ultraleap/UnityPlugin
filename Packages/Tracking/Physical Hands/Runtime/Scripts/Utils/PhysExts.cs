using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.PhysicalHands
{
    public static class PhysExts
    {
        internal static int OverlapBoxNonAlloc(BoxCollider box, Collider[] results, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return OverlapBoxNonAllocOffset(box, Vector3.zero, results, layerMask, queryTriggerInteraction);
        }

        internal static int OverlapBoxNonAllocOffset(BoxCollider box, Vector3 offset, Collider[] results, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal, float extraRadius = 0)
        {
            Vector3 center, halfExtents;
            Quaternion orientation;
            box.ToWorldSpaceBoxOffset(offset, out center, out halfExtents, out orientation, extraRadius);
            return Physics.OverlapBoxNonAlloc(center, halfExtents, results, orientation, layerMask, queryTriggerInteraction);
        }

        internal static void ToWorldSpaceBox(this BoxCollider box, out Vector3 center, out Vector3 halfExtents, out Quaternion orientation)
        {
            ToWorldSpaceBoxOffset(box, Vector3.zero, out center, out halfExtents, out orientation);
        }

        internal static void ToWorldSpaceBoxOffset(this BoxCollider box, Vector3 offset, out Vector3 center, out Vector3 halfExtents, out Quaternion orientation, float extraRadius = 0)
        {
            orientation = box.transform.rotation;
            center = box.transform.TransformPoint(box.center + offset);
            var lossyScale = box.transform.lossyScale;
            var scale = AbsVec3(lossyScale);
            halfExtents = (Vector3.Scale(scale, box.size) * 0.5f) + (extraRadius == 0 ? Vector3.zero : new Vector3(extraRadius, extraRadius, extraRadius));
        }

        internal static int OverlapSphereNonAlloc(SphereCollider sphere, Collider[] results, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            Vector3 center;
            float radius;
            sphere.ToWorldSpaceSphere(out center, out radius);
            return Physics.OverlapSphereNonAlloc(center, radius, results, layerMask, queryTriggerInteraction);
        }

        internal static void ToWorldSpaceSphere(this SphereCollider sphere, out Vector3 center, out float radius)
        {
            center = sphere.transform.TransformPoint(sphere.center);
            radius = sphere.radius * MaxVec3(AbsVec3(sphere.transform.lossyScale));
        }

        internal static int OverlapCapsuleNonAlloc(CapsuleCollider capsule, Collider[] results, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return OverlapCapsuleNonAllocOffset(capsule, Vector3.zero, results, layerMask, queryTriggerInteraction);
        }

        internal static int OverlapCapsuleNonAllocOffset(CapsuleCollider capsule, Vector3 offset, Collider[] results, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal, float extraRadius = 0)
        {
            Vector3 point0, point1;
            float radiusOut;
            capsule.ToWorldSpaceCapsuleOffset(offset, out point0, out point1, out radiusOut);
            return Physics.OverlapCapsuleNonAlloc(point0, point1, extraRadius + radiusOut, results, layerMask, queryTriggerInteraction);
        }

        internal static void ToWorldSpaceCapsule(this CapsuleCollider capsule, out Vector3 point0, out Vector3 point1, out float radius)
        {
            ToWorldSpaceCapsuleOffset(capsule, Vector3.zero, out point0, out point1, out radius);
        }

        internal static void ToWorldSpaceCapsuleOffset(this CapsuleCollider capsule, Vector3 offset, out Vector3 point0, out Vector3 point1, out float radius)
        {
            var center = capsule.transform.TransformPoint(capsule.center + offset);
            radius = 0f;
            float height = 0f;
            Vector3 lossyScale = AbsVec3(capsule.transform.lossyScale);
            Vector3 dir = Vector3.zero;

            switch (capsule.direction)
            {
                case 0: // x
                    radius = Mathf.Max(lossyScale.y, lossyScale.z) * capsule.radius;
                    height = lossyScale.x * capsule.height;
                    dir = capsule.transform.TransformDirection(Vector3.right);
                    break;
                case 1: // y
                    radius = Mathf.Max(lossyScale.x, lossyScale.z) * capsule.radius;
                    height = lossyScale.y * capsule.height;
                    dir = capsule.transform.TransformDirection(Vector3.up);
                    break;
                case 2: // z
                    radius = Mathf.Max(lossyScale.x, lossyScale.y) * capsule.radius;
                    height = lossyScale.z * capsule.height;
                    dir = capsule.transform.TransformDirection(Vector3.forward);
                    break;
            }

            if (height < radius * 2f)
            {
                dir = Vector3.zero;
            }

            point0 = center + dir * (height * 0.5f - radius);
            point1 = center - dir * (height * 0.5f - radius);
        }

        internal static Vector3 AbsVec3(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        internal static float MaxVec3(Vector3 v)
        {
            return Mathf.Max(v.x, Mathf.Max(v.y, v.z));
        }

        internal static bool ContainsRange<T>(this T[] arr, T value, int maxIndex)
        {
            return System.Array.IndexOf(arr, value, 0, maxIndex) != -1;
        }

        /// <summary>
        /// Checks if a point is within a collider
        /// </summary>
        /// <param name="collider">The collider to check</param>
        /// <param name="point">The point to check</param>
        /// <returns></returns>
        internal static bool IsPointWithinCollider(this Collider collider, Vector3 point)
        {
            return collider.IsSphereWithinCollider(point, 0);
        }

        /// <summary>
        /// Checks if a sphere intersects with a collider
        /// </summary>
        /// <param name="collider">The collider to check</param>
        /// <param name="centre">The centre of the sphere</param>
        /// <param name="radius">The radius of the sphere, in meters</param>
        /// <returns></returns>
        internal static bool IsSphereWithinCollider(this Collider collider, Vector3 centre, float radius)
        {
            Vector3 closestPoint = collider.ClosestPoint(centre);
            bool isPointWithinCollider = closestPoint == centre;

            if (isPointWithinCollider)
            {
                return true;
            }
            else
            {
                return Vector3.Distance(centre, closestPoint) < radius;
            }
        }
    }
}