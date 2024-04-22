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
    public static class ContactUtils
    {
        // Magic 0th thumb bone dataRotation offsets from LeapC
        // TODO: Remove this and make it non-necessary
        internal const float HAND_ROTATION_OFFSET_Y = 25.9f, HAND_ROTATION_OFFSET_Z = -63.45f;

        internal static Vector3 CalculateAverageKnucklePosition(this Hand hand)
        {
            return (hand.Fingers[1].bones[0].NextJoint +
                hand.Fingers[2].bones[0].NextJoint +
                hand.Fingers[3].bones[0].NextJoint +
                hand.Fingers[4].bones[0].NextJoint) / 4;
        }

        internal static Vector3 CalculatePalmSize(Hand hand)
        {
            return new Vector3(hand.PalmWidth, hand.Fingers[2].Bone(0).Width, Vector3.Distance(CalculateAverageKnucklePosition(hand), hand.WristPosition));
        }

        internal static void SetupPalmCollider(BoxCollider collider, CapsuleCollider[] palmEdges, Hand hand, PhysicMaterial material = null)
        {
            Vector3 palmSize = CalculatePalmSize(hand);
            if (palmEdges != null)
            {
                // This code places colliders around the palm to get a better representation of the palm than a single box collider
                collider.center = new Vector3(0f, palmSize.y * 0.1f, -0.015f);
                collider.size = new Vector3(palmSize.x - palmSize.y, palmSize.y - (palmSize.y * 0.2f), palmSize.z - (palmSize.y / 2f));

                palmEdges[0].direction = 0;
                palmEdges[0].radius = palmSize.y / 2f;
                palmEdges[0].height = palmSize.x;
                palmEdges[0].center = new Vector3(0, 0, collider.center.z - (collider.size.z / 2f));

                palmEdges[1].direction = 2;
                palmEdges[1].radius = palmSize.y / 2f;
                palmEdges[1].height = collider.size.z + (palmSize.y / 2f);
                palmEdges[1].center = new Vector3(collider.size.x / 2f, 0, collider.center.z - (palmSize.y / 4f));

                palmEdges[2].direction = 2;
                palmEdges[2].radius = palmSize.y / 2f;
                palmEdges[2].height = collider.size.z + (palmSize.y / 2f);
                palmEdges[2].center = new Vector3(-collider.size.x / 2f, 0, collider.center.z - (palmSize.y / 4f));

                if (material != null)
                {
                    for (int i = 0; i < palmEdges.Length; i++)
                    {
                        palmEdges[i].material = material;
                    }
                }
            }
            else
            {
                collider.center = new Vector3(0, 0, -0.015f);
                collider.size = palmSize;
            }

            if (material != null)
            {
                collider.material = material;
            }
        }

        internal static void InterpolatePalmBones(BoxCollider collider, CapsuleCollider[] palmEdges, Hand hand, float interp)
        {
            Vector3 palmSize = CalculatePalmSize(hand);
            if (palmEdges != null)
            {
                collider.center = Vector3.Lerp(collider.center, new Vector3(0f, palmSize.y * 0.1f, -0.015f), interp);
                collider.size = Vector3.Lerp(collider.size, new Vector3(palmSize.x - palmSize.y, palmSize.y - (palmSize.y * 0.2f), palmSize.z - (palmSize.y / 2f)), interp);

                palmEdges[0].radius = Mathf.Lerp(palmEdges[0].radius, palmSize.y / 2f, interp);
                palmEdges[0].height = Mathf.Lerp(palmEdges[0].height, palmSize.x, interp);
                palmEdges[0].center = Vector3.Lerp(palmEdges[0].center, new Vector3(0, 0, collider.center.z - (collider.size.z / 2f)), interp);

                palmEdges[1].radius = Mathf.Lerp(palmEdges[1].radius, palmSize.y / 2f, interp);
                palmEdges[1].height = Mathf.Lerp(palmEdges[1].height, collider.size.z + (palmSize.y / 2f), interp);
                palmEdges[1].center = Vector3.Lerp(palmEdges[1].center, new Vector3(collider.size.x / 2f, 0, collider.center.z - (palmSize.y / 4f)), interp);

                palmEdges[2].radius = Mathf.Lerp(palmEdges[2].radius, palmSize.y / 2f, interp);
                palmEdges[2].height = Mathf.Lerp(palmEdges[2].height, collider.size.z + (palmSize.y / 2f), interp);
                palmEdges[2].center = Vector3.Lerp(palmEdges[2].center, new Vector3(-collider.size.x / 2f, 0, collider.center.z - (palmSize.y / 4f)), interp);
            }
            else
            {
                collider.center = new Vector3(0f, 0, -0.015f);
                collider.size = Vector3.Lerp(collider.size, palmSize, interp);
            }
        }

        internal static void SetupBoneCollider(CapsuleCollider collider, Bone bone, PhysicMaterial material = null)
        {
            collider.direction = 2;
            if (bone.Width <= 0) { bone.Width = 0.01f; }
            if (bone.Length <= 0) { bone.Length = 0.01f; }
            collider.radius = bone.Width * 0.5f;
            collider.height = bone.Length + bone.Width;
            collider.center = new Vector3(0f, 0f, bone.Length / 2f);
            if (material != null)
            {
                collider.material = material;
            }
        }

        internal static Vector3 ToLinearVelocity(Vector3 deltaPosition, float deltaTime)
        {
            return deltaPosition / deltaTime;
        }

        internal static Vector3 ToLinearVelocity(Vector3 startPosition, Vector3 destinationPosition, float deltaTime)
        {
            return ToLinearVelocity(destinationPosition - startPosition, deltaTime);
        }

        internal static Vector3 ToAngularVelocity(Quaternion deltaRotation, float deltaTime)
        {
            Vector3 deltaAxis;
            float deltaAngle;
            deltaRotation.ToAngleAxis(out deltaAngle, out deltaAxis);

            if (float.IsInfinity(deltaAxis.x))
            {
                deltaAxis = Vector3.zero;
                deltaAngle = 0;
            }

            if (deltaAngle > 180)
            {
                deltaAngle -= 360.0f;
            }

            return deltaAxis * deltaAngle * Mathf.Deg2Rad / deltaTime;
        }

        internal static Vector3 ToAngularVelocity(Quaternion startRotation, Quaternion destinationRotation, float deltaTime)
        {
            return ToAngularVelocity(destinationRotation * Quaternion.Inverse(startRotation), deltaTime);
        }

        internal static bool IsValid(this Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)) && !(float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        }

        internal static Vector3 InverseTransformPoint(Vector3 transformPos, Quaternion transformRotation, Vector3 transformScale, Vector3 pos)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(transformPos, transformRotation, transformScale);
            Matrix4x4 inverse = matrix.inverse;
            return inverse.MultiplyPoint3x4(pos);
        }

        internal static Vector3 InverseTransformPoint(Vector3 transformPos, Quaternion transformRotation, Vector3 pos)
        {
            return InverseTransformPoint(transformPos, transformRotation, Vector3.one, pos);
        }

        internal static float EaseOut(this float input)
        {
            return input.Flip().Square().Flip();
        }

        internal static float Square(this float input)
        {
            return input * input;
        }

        internal static float Flip(this float input)
        {
            return 1 - input;
        }

        /// <summary>
        /// Checks whether any of the following are currently inside an object:
        /// - tip of the bone
        /// - centre of the bone
        /// - midpoint of tip & centre
        /// - midpoint of centre & base
        /// 
        /// To take into account width, pass in the bone width
        /// </summary>
        /// <param name="bone">The bone to check if it is in</param>
        /// <param name="colliders">An array of colliders representing the object to check</param>
        /// <param name="boneWidth">Optional width of the bone - if not passed, will default to 0</param>
        /// <returns></returns>
        internal static bool IsBoneWithinObject(this Leap.Bone bone, Collider[] colliders, float boneWidth = 0)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null)
                {
                    continue;
                }
                if (colliders[i].IsSphereWithinCollider(bone.NextJoint, boneWidth)
                    || colliders[i].IsSphereWithinCollider(bone.Center, boneWidth)
                    || colliders[i].IsSphereWithinCollider(Vector3.Lerp(bone.Center, bone.NextJoint, 0.5f), boneWidth)
                    || colliders[i].IsSphereWithinCollider(Vector3.Lerp(bone.Center, bone.PrevJoint, 0.5f), boneWidth)
                    )
                {
                    return true;
                }
            }
            return false;
        }

        internal static Vector3 GetClosestPointOnFiniteLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 lineDir = lineEnd - lineStart;
            float lineLength = lineDir.magnitude;
            lineDir.Normalize();
            float projectLength = Mathf.Clamp(Vector3.Dot(point - lineStart, lineDir), 0f, lineLength);
            return lineStart + lineDir * projectLength;
        }

        internal static Vector3 GetClosestPointOnLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 lineDir = lineEnd - lineStart;
            lineDir.Normalize();
            float dot = Vector3.Dot(point - lineStart, lineDir);
            return lineStart + lineDir * dot;
        }

        /// <summary>
        /// Takes the center of the box and the 4 points of the halfExtents, scaled down, and then performs ClosestPoint checks relative to them. 
        /// Reduce one of your halfExtents axes to perform this as a 2D rectangle.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="orientation"></param>
        /// <param name="halfExtents"></param>
        /// <param name="collider">The collider you want to test against</param>
        /// <param name="axis">Specifies which axis the rectangle sits across. Defaults to Y.</param>
        /// <returns></returns>
        internal static Vector3 ClosestPointEstimationFromRectangle(Vector3 center, Quaternion orientation, Vector2 halfExtents, Collider collider, int axis = 1)
        {
            float distance = float.MaxValue, tempDist;
            Vector3 returnPoint = Vector3.zero, tempPoint, extents;

            tempPoint = collider.ClosestPoint(center);
            tempDist = Vector3.Distance(tempPoint, center);
            if (tempDist < distance)
            {
                returnPoint = tempPoint;
            }

            halfExtents *= 0.66f;
            for (int i = 0; i < 4; i++)
            {
                switch (axis)
                {
                    // X
                    case 0:
                        extents = new Vector3(0, i / 2 < 1 ? halfExtents.x : -halfExtents.x, i % 2 == 0 ? halfExtents.y : -halfExtents.y);
                        break;
                    // Y
                    default:
                    case 1:
                        extents = new Vector3(i / 2 < 1 ? halfExtents.x : -halfExtents.x, 0, i % 2 == 0 ? halfExtents.y : -halfExtents.y);
                        break;
                    // Z
                    case 2:
                        extents = new Vector3(i / 2 < 1 ? halfExtents.x : -halfExtents.x, i % 2 == 0 ? halfExtents.y : -halfExtents.y, 0);
                        break;
                }
                tempPoint = collider.ClosestPoint(center + (orientation * extents));
                tempDist = Vector3.Distance(tempPoint, center);
                if (tempDist < distance)
                {
                    returnPoint = tempPoint;
                }
            }
            return returnPoint;
        }

        /// <summary>
        /// Check if a point is inside the points of the face and then modify it if it's not. You must enter the points in the correct order of the face.
        /// </summary>
        internal static Vector3 ClosestPointToRectangleFace(Vector3[] face, Vector3 point)
        {
            // 1_ get sqrDist to closest face point
            var closestDistance = float.MaxValue;
            var chosenIndex = -1;
            for (var i = 0; i < face.Length; i++)
            {
                var f = face[i];
                var sqrDist = Vector3.SqrMagnitude(f - point);
                if (sqrDist < closestDistance)
                {
                    chosenIndex = i;
                    closestDistance = sqrDist;
                }
            }

            // 2_ get the distance to the 2 neighbour points of the face's closest point.
            // if we have a smaller distance to the 2 neighbour points than the distance between the chosen point and the neighbour point: we are inside the face.
            var chosenPoint = face[chosenIndex];

            var nextNeighbour = face[(chosenIndex + 1) % face.Length];
            var prevNeighbour = face[(-1 + chosenIndex + face.Length) % (face.Length)];

            // We are inside of the face.
            if (GetClosestPointOnLine(point, nextNeighbour, chosenPoint).IsBetween(chosenPoint, nextNeighbour) &&
                GetClosestPointOnLine(point, prevNeighbour, chosenPoint).IsBetween(chosenPoint, prevNeighbour))
            {
                return point;
            }
            else // we are outside the face! That means the closest point is necessarily on the lines defined by the 4 corners points of the face.
            {
                var closestSqrDistance = float.MaxValue;
                var closestPoint = Vector3.zero;
                for (var i = 0; i < face.Length; i++)
                {
                    var prevNearestPoint = GetClosestPointOnFiniteLine(point, face[i], face[(-1 + chosenIndex + face.Length) % (face.Length)]);
                    var nextNearestPoint = GetClosestPointOnFiniteLine(point, face[i], face[(chosenIndex + 1) % face.Length]);

                    var distanceToNextNearestPoint = Vector3.SqrMagnitude(nextNearestPoint - point);
                    if (distanceToNextNearestPoint < closestSqrDistance)
                    {
                        closestPoint = nextNearestPoint;
                        closestSqrDistance = distanceToNextNearestPoint;
                    }
                    var distanceToPrevNearestPoint = Vector3.SqrMagnitude(prevNearestPoint - point);
                    if (distanceToPrevNearestPoint < closestSqrDistance)
                    {
                        closestPoint = prevNearestPoint;
                        closestSqrDistance = distanceToPrevNearestPoint;
                    }
                }

                return closestPoint;
            }
        }

        internal static float SqrDist(Vector3 a, Vector3 b)
        {
            return Vector3.SqrMagnitude(a - b);
        }

        internal static bool IsBetween(this Vector3 a, Vector3 b, Vector3 c)
        {
            var totalDist = SqrDist(b, c);
            if (SqrDist(a, b) <= totalDist && SqrDist(a, c) <= totalDist)
            {
                return true;
            }
            else return false;
        }
    }
}