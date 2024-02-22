/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.PhysicalHands
{
    public static class PhysExts
    {
        /// <summary>
        /// Computes and stores colliders touching or inside the box into the provided buffer.
        /// Does not attempt to grow the buffer if it runs out of space.
        /// </summary>
        /// <param name="box">Which box should be checked</param>
        /// <param name="results">Array of colliders which are inside this box</param>
        /// <param name="layerMask">Which layermask should be used</param>
        /// <param name="queryTriggerInteraction">Overrides global Physics.queriesHitTriggers</param>
        /// <returns>Returns the amount of colliders stored into the results buffer.</returns>
        public static int OverlapBoxNonAlloc(BoxCollider box, Collider[] results, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return OverlapBoxNonAllocOffset(box, Vector3.zero, results, layerMask, queryTriggerInteraction);
        }

        /// <summary>
        /// Computes and stores colliders touching or inside the offset box into the provided buffer.
        /// Does not attempt to grow the buffer if it runs out of space.
        /// </summary>
        /// <param name="box">Which box should be checked</param>
        /// <param name="offset">Offset to move the box by.</param>
        /// <param name="results">Array of colliders which are inside this box</param>
        /// <param name="layerMask">Which layermask should be used</param>
        /// <param name="queryTriggerInteraction">Overrides global Physics.queriesHitTriggers</param>
        /// <returns>Returns the amount of colliders stored into the results buffer.</returns>
        public static int OverlapBoxNonAllocOffset(BoxCollider box, Vector3 offset, Collider[] results, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal, float extraRadius = 0)
        {
            Vector3 center, halfExtents;
            Quaternion orientation;
            box.ToWorldSpaceBoxOffset(offset, out center, out halfExtents, out orientation, extraRadius);
            return Physics.OverlapBoxNonAlloc(center, halfExtents, results, orientation, layerMask, queryTriggerInteraction);
        }

        /// <summary>
        /// Computes where the box is in world space.
        /// </summary>
        /// <param name="box">Which box should be checked.</param>
        /// <param name="center">Where is the centre of the box in world space.</param>
        /// <param name="halfExtents">Half extents of the box in world space.</param>
        /// <param name="orientation">Which way does the box face in world space.</param>
        public static void ToWorldSpaceBox(this BoxCollider box, out Vector3 center, out Vector3 halfExtents, out Quaternion orientation)
        {
            ToWorldSpaceBoxOffset(box, Vector3.zero, out center, out halfExtents, out orientation);
        }

        /// <summary>
        /// Computes where the box is in world space with an offset.
        /// </summary>
        /// <param name="box">Which box should be checked.</param>
        /// <param name="offset">Offset at which the box should be calculated.</param>
        /// <param name="center">Where is the centre of the box in world space.</param>
        /// <param name="halfExtents">Half extents of the box in world space.</param>
        /// <param name="orientation">Which way does the box face in world space.</param>
        public static void ToWorldSpaceBoxOffset(this BoxCollider box, Vector3 offset, out Vector3 center, out Vector3 halfExtents, out Quaternion orientation, float extraRadius = 0)
        {
            orientation = box.transform.rotation;
            center = box.transform.TransformPoint(box.center + offset);
            var lossyScale = box.transform.lossyScale;
            var scale = AbsVec3(lossyScale);
            halfExtents = (Vector3.Scale(scale, box.size) * 0.5f) + (extraRadius == 0 ? Vector3.zero : new Vector3(extraRadius, extraRadius, extraRadius));
        }

        /// <summary>
        /// Computes and stores colliders touching or inside the sphere into the provided buffer.
        /// Does not attempt to grow the buffer if it runs out of space.The length of the buffer is returned when the buffer is full.
        /// </summary>
        /// <param name="sphere">Which sphere collider should be checked</param>
        /// <param name="results">Array of colliders which are inside this sphere</param>
        /// <param name="layerMask">Which layermask should be used</param>
        /// <param name="queryTriggerInteraction">Overrides global Physics.queriesHitTriggers</param>
        public static int OverlapSphereNonAlloc(SphereCollider sphere, Collider[] results, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            Vector3 center;
            float radius;
            sphere.ToWorldSpaceSphere(out center, out radius);
            return Physics.OverlapSphereNonAlloc(center, radius, results, layerMask, queryTriggerInteraction);
        }

        /// <summary>
        /// Computes where the sphere is in world space.
        /// </summary>
        /// <param name="sphere">Which box should be checked.</param>
        /// <param name="center">Where is the centre of the box in world space.</param>
        /// <param name="radius">The radius of the calculated sphere.</param>
        public static void ToWorldSpaceSphere(this SphereCollider sphere, out Vector3 center, out float radius)
        {
            center = sphere.transform.TransformPoint(sphere.center);
            radius = sphere.radius * MaxVec3(AbsVec3(sphere.transform.lossyScale));
        }

        /// <summary>
        /// Computes and stores colliders touching or inside the capsule into the provided buffer.
        /// Does not attempt to grow the buffer if it runs out of space. The length of the buffer is returned when the buffer is full.
        /// </summary>
        /// <param name="capsule">Which capsule collider should be checked</param>
        /// <param name="results">Array of colliders which are inside this capsule</param>
        /// <param name="layerMask">Which layermask should be used</param>
        /// <param name="queryTriggerInteraction">Overrides global Physics.queriesHitTriggers</param>
        public static int OverlapCapsuleNonAlloc(CapsuleCollider capsule, Collider[] results, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return OverlapCapsuleNonAllocOffset(capsule, Vector3.zero, results, layerMask, queryTriggerInteraction);
        }

        /// <summary>
        /// Computes and stores colliders touching or inside the capsule into the provided buffer.
        /// Does not attempt to grow the buffer if it runs out of space. The length of the buffer is returned when the buffer is full.
        /// </summary>
        /// <param name="capsule">Which capsule collider should be checked</param>
        /// <param name="offset">Offset at which the capsule should be calculated.</param>
        /// <param name="results">Array of colliders which are inside this capsule</param>
        /// <param name="layerMask">Which layermask should be used</param>
        /// <param name="queryTriggerInteraction">Overrides global Physics.queriesHitTriggers</param>
        public static int OverlapCapsuleNonAllocOffset(CapsuleCollider capsule, Vector3 offset, Collider[] results, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal, float extraRadius = 0)
        {
            Vector3 point0, point1;
            float radiusOut;
            capsule.ToWorldSpaceCapsuleOffset(offset, out point0, out point1, out radiusOut);
            return Physics.OverlapCapsuleNonAlloc(point0, point1, extraRadius + radiusOut, results, layerMask, queryTriggerInteraction);
        }

        /// <summary>
        /// Computes where the sphere is in world space.
        /// </summary>
        /// <param name="capsule">Which capsule should be checked.</param>
        /// <param name="point0">Point at the centre of one of the end spheres</param>
        /// <param name="point1">Point at the centre of the other end sphere</param>
        /// <param name="radius">Radius of the capsule.</param>
        public static void ToWorldSpaceCapsule(this CapsuleCollider capsule, out Vector3 point0, out Vector3 point1, out float radius)
        {
            ToWorldSpaceCapsuleOffset(capsule, Vector3.zero, out point0, out point1, out radius);
        }

        /// <summary>
        /// Computes where the sphere is in world space.
        /// </summary>
        /// <param name="capsule">Which capsule should be checked.</param>
        /// <param name="offset">Offset to move the capsule by..</param>
        /// <param name="point0">Point at the centre of one of the end spheres of the capsule</param>
        /// <param name="point1">Point at the centre of the other end sphere of the capsule</param>
        /// <param name="radius">Radius of the capsule.</param>
        public static void ToWorldSpaceCapsuleOffset(this CapsuleCollider capsule, Vector3 offset, out Vector3 point0, out Vector3 point1, out float radius)
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

        /// <summary>
        /// Get the absolute values of x, y or z in this vector3.
        /// </summary>
        /// <param name="v">Vector3 to check</param>
        /// <returns>Returns the absolute value of each element in a new Vector3</returns>
        public static Vector3 AbsVec3(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        /// <summary>
        /// Get the highest value: x, y or z in this vector3.
        /// </summary>
        /// <param name="v">Vector3 to check</param>
        /// <returns>Highest value</returns>
        public static float MaxVec3(Vector3 v)
        {
            return Mathf.Max(v.x, Mathf.Max(v.y, v.z));
        }

        /// <summary>
        /// Does this array contain the value.
        /// </summary>
        /// <param name="arr">Array to check</param>
        /// <param name="value">Value to search for in the array</param>
        /// <param name="maxIndex">How far through the array should we search</param>
        /// <returns>bool</returns>
        public static bool ContainsRange<T>(this T[] arr, T value, int maxIndex)
        {
            return System.Array.IndexOf(arr, value, 0, maxIndex) != -1;
        }

        /// <summary>
        /// Checks if a point is within a collider
        /// </summary>
        /// <param name="collider">The collider to check</param>
        /// <param name="point">The point to check</param>
        /// <returns></returns>
        public static bool IsPointWithinCollider(this Collider collider, Vector3 point)
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
        public static bool IsSphereWithinCollider(this Collider collider, Vector3 centre, float radius)
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