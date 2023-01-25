/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/


using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Interaction.Internal.InteractionEngineUtility
{
    public static class ColliderUtil
    {

        //*************
        //GENRAL HELPER

        /// <summary>
        /// Returns whether or not a given vector is within a given radius to a center point
        /// </summary>
        public static bool WithinDistance(Vector3 point, Vector3 center, float radius)
        {
            return (point - center).sqrMagnitude < radius * radius;
        }

        public static bool WithinDistance(Vector3 pointToOrigin, float radius)
        {
            return pointToOrigin.sqrMagnitude < radius * radius;
        }

        /// <summary>
        /// Given a point and a center, return a new point that is along the axis created by the 
        /// two vectors, and is a given distance from the center
        /// </summary>
        public static Vector3 GetPointAtDistance(Vector3 point, Vector3 center, float distance)
        {
            return (point - center).normalized * distance + center;
        }

        /// <summary>
        /// Given a collider and a position relative to the colliders transform, return whether or not the 
        /// position lies within the collider.  Can also optionally extrude the collider.
        /// 
        /// Warning: MeshColliders are not fully supported; this will only support testing against a MeshCollider's
        /// bounding box, with no extrusion.
        /// </summary>
        public static bool IsPointInside(this Collider collider, Vector3 localPosition, float extrude = 0.0f)
        {
            if (collider is SphereCollider) return (collider as SphereCollider).IsPointInside(localPosition, extrude);
            if (collider is BoxCollider) return (collider as BoxCollider).IsPointInside(localPosition, extrude);
            if (collider is CapsuleCollider) return (collider as CapsuleCollider).IsPointInside(localPosition, extrude);
            if (collider is MeshCollider) return collider.bounds.Contains(collider.transform.TransformPoint(localPosition));

            throw nullOrInvalidException(collider);
        }

        /// <summary>
        /// Given a collider and a position relative to the colliders transform, return the point on the surface 
        /// of the collider closest to the position.  Can also optionally extrude the collider.
        /// 
        /// Warning: If you are using a version of Unity pre-5.6, MeshColliders are not fully supported; this will
        /// only find the closest point on a MeshCollider's bounding box, with no extrusion.
        /// </summary>
        public static Vector3 ClosestPointOnSurface(this Collider collider, Vector3 localPosition, float extrude = 0.0f)
        {
            if (collider is SphereCollider) return (collider as SphereCollider).ClosestPointOnSurface(localPosition, extrude);
            if (collider is BoxCollider) return (collider as BoxCollider).ClosestPointOnSurface(localPosition, extrude);
            if (collider is CapsuleCollider) return (collider as CapsuleCollider).ClosestPointOnSurface(localPosition, extrude);
            if (collider is MeshCollider) return collider.transform.InverseTransformPoint(collider.ClosestPointOnBounds(collider.transform.TransformPoint(localPosition)));

            throw nullOrInvalidException(collider);
        }

        /// <summary>
        /// Given a list of colliders and a position in global space, return whether or not the position lies
        /// within any of the colliders.  Can also optionally extrude the collider.
        /// </summary>
        public static bool IsPointInside(List<Collider> colliders, Vector3 globalPosition, float extrude = 0.0f)
        {
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider collider = colliders[i];
                Vector3 localPosition = collider.transform.InverseTransformPoint(globalPosition);
                if (collider.IsPointInside(localPosition, extrude))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Given a list of colliders and a position in global space, return the point on the surface of any
        /// of the colliders that is closest to the given position.  NOTE that this point might be inside of 
        /// a collider!  Can also optionally extrude the colliders.
        /// </summary>
        public static Vector3 ClosestPointOnSurfaces(List<Collider> colliders, Vector3 globalPosition, float extrude = 0.0f)
        {
            Transform chosenTransform = null;
            Vector3 closestPoint = Vector3.zero;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < colliders.Count; i++)
            {
                Collider collider = colliders[i];
                Vector3 localPoint = collider.transform.InverseTransformPoint(globalPosition);
                Vector3 point = collider.ClosestPointOnSurface(localPoint, extrude);
                float distance = (globalPosition - point).sqrMagnitude;
                if (distance < closestDistance)
                {
                    chosenTransform = collider.transform;
                    closestDistance = distance;
                    closestPoint = point;
                }
            }

            return chosenTransform.TransformPoint(closestPoint);
        }

        //***************
        //Raycasting

        public static bool SegmentCast(List<Collider> colliders, Vector3 worldStart, Vector3 worldEnd, out RaycastHit hitInfo)
        {
            Ray ray = new Ray(worldStart, worldEnd - worldStart);
            float dist = Vector3.Distance(worldStart, worldEnd);

            hitInfo = new RaycastHit();

            bool hitAny = false;
            RaycastHit tempHit;
            foreach (Collider collider in colliders)
            {
                if (collider.Raycast(ray, out tempHit, dist))
                {
                    if (!hitAny || tempHit.distance < hitInfo.distance)
                    {
                        hitInfo = tempHit;
                        hitAny = true;
                    }
                }
            }

            return hitAny;
        }

        //***************
        //SPHERE COLLIDER

        public static bool IsPointInside(this SphereCollider collider, Vector3 localPosition, float extrude = 0.0f)
        {
            localPosition -= collider.center;
            return WithinDistance(localPosition, collider.radius + extrude);
        }

        public static Vector3 ClosestPointOnSurface(this SphereCollider collider, Vector3 localPosition, float extrude = 0.0f)
        {
            return GetPointAtDistance(localPosition, collider.center, collider.radius + extrude);
        }

        //************
        //BOX COLLIDER

        public static bool IsPointInside(this BoxCollider collider, Vector3 localPosition, float extrude = 0.0f)
        {
            localPosition -= collider.center;
            if (Mathf.Abs(localPosition.x) > (collider.size.x / 2.0f) + (extrude / collider.transform.lossyScale.x))
            {
                return false;
            }
            if (Mathf.Abs(localPosition.y) > (collider.size.y / 2.0f) + (extrude / collider.transform.lossyScale.y))
            {
                return false;
            }
            if (Mathf.Abs(localPosition.z) > (collider.size.z / 2.0f) + (extrude / collider.transform.lossyScale.z))
            {
                return false;
            }
            return true;
        }

        public static Vector3 ClosestPointOnSurface(this BoxCollider collider, Vector3 localPosition, float extrude = 0.0f)
        {
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
                Mathf.Abs(localPosition.z) < radius.z)
            {
                //Snap closest axis to the bounds
                //Farthest from the center
                if (Mathf.Abs(localPosition.x) > Mathf.Abs(localPosition.y))
                {
                    if (Mathf.Abs(localPosition.x) > Mathf.Abs(localPosition.z))
                    {
                        //x is farthest
                        localPosition.x = (localPosition.x > 0) ? radius.x : -radius.x;
                    }
                    else
                    {
                        //z is farthest
                        localPosition.z = (localPosition.z > 0) ? radius.z : -radius.z;
                    }
                }
                else
                {
                    if (Mathf.Abs(localPosition.y) > Mathf.Abs(localPosition.z))
                    {
                        //y is farthest
                        localPosition.y = (localPosition.y > 0) ? radius.y : -radius.y;
                    }
                    else
                    {
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

        /// <summary>
        /// A capsule is defined as a line segment with a radius.  This method returns the two endpoints 
        /// of that segment, as well as it's length, in local space.
        /// </summary>
        public static void GetSegmentInfo(this CapsuleCollider collider, out Vector3 localV0, out Vector3 localV1, out float length)
        {
            Vector3 axis = Vector3.right;
            if (collider.direction == 1)
            {
                axis = Vector3.up;
            }
            else
            {
                axis = Vector3.forward;
            }

            length = Mathf.Max(0, collider.height - collider.radius * 2);
            localV0 = axis * length / 2.0f + collider.center;
            localV1 = -axis * length / 2.0f + collider.center;
        }

        public static bool IsPointInside(this CapsuleCollider collider, Vector3 localPosition, float tolerance = 0.0f)
        {
            Vector3 v0, v1;
            float length;
            collider.GetSegmentInfo(out v0, out v1, out length);

            if (length == 0.0f)
            {
                return WithinDistance(localPosition, collider.radius + tolerance);
            }

            float t = Vector3.Dot(localPosition - v0, v1 - v0) / (length * length);
            if (t <= 0.0f)
            {
                return WithinDistance(localPosition, v0, collider.radius + tolerance);
            }
            if (t >= 1.0f)
            {
                return WithinDistance(localPosition, v1, collider.radius + tolerance);
            }

            Vector3 projection = v0 + t * (v1 - v0);
            return WithinDistance(localPosition, projection, collider.radius + tolerance);
        }

        public static Vector3 ClosestPointOnSurface(this CapsuleCollider collider, Vector3 localPosition, float extrude = 0.0f)
        {
            Vector3 v0, v1;
            float length;
            collider.GetSegmentInfo(out v0, out v1, out length);

            if (length == 0.0f)
            {
                return GetPointAtDistance(localPosition, collider.center, collider.radius);
            }

            float t = Vector3.Dot(localPosition - v0, v1 - v0) / (length * length);
            if (t <= 0.0f)
            {
                return GetPointAtDistance(localPosition, v0, collider.radius + extrude);
            }
            if (t >= 1.0f)
            {
                return GetPointAtDistance(localPosition, v1, collider.radius + extrude);
            }

            Vector3 projection = v0 + t * (v1 - v0);
            return GetPointAtDistance(localPosition, projection, collider.radius + extrude);
        }

        public static float DistanceToSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            float t = Vector3.Dot(p - a, b - a) / Vector3.Dot(a - b, a - b);
            if (t <= 0.0f)
            {
                return Vector3.Distance(p, a);
            }
            if (t >= 1.0f)
            {
                return Vector3.Distance(p, b);
            }
            Vector3 projection = a + t * (b - a);
            return Vector3.Distance(p, projection);
        }

        private static Exception nullOrInvalidException(Collider collider)
        {
            if (collider == null)
            {
                return new ArgumentNullException();
            }
            else
            {
                return new ArgumentException("Collider type of " + collider.GetType() + " is not supported.");
            }
        }
    }
}