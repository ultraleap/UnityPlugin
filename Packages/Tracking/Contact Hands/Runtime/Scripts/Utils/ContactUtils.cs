using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public static class ContactUtils
    {

        public static Vector3 CalculateAverageKnucklePosition(this Hand hand)
        {
            return (hand.Fingers[1].bones[0].NextJoint +
                hand.Fingers[2].bones[0].NextJoint +
                hand.Fingers[3].bones[0].NextJoint +
                hand.Fingers[4].bones[0].NextJoint) / 4;
        }

        public static Vector3 CalculatePalmSize(Hand hand)
        {
            return new Vector3(hand.PalmWidth, hand.Fingers[2].Bone(0).Width, Vector3.Distance(CalculateAverageKnucklePosition(hand), hand.WristPosition));
        }

        public static void SetupPalmCollider(BoxCollider collider, Hand hand, PhysicMaterial material = null)
        {
            collider.center = new Vector3(0f, 0f, -0.015f);
            collider.size = CalculatePalmSize(hand);
            if (material != null)
            {
                collider.material = material;
            }
        }

        public static void SetupBoneCollider(CapsuleCollider collider, Bone bone, PhysicMaterial material = null)
        {
            collider.direction = 2;
            collider.radius = bone.Width * 0.5f;
            collider.height = bone.Length + bone.Width;
            collider.center = new Vector3(0f, 0f, bone.Length / 2f);
            if (material != null)
            {
                collider.material = material;
            }
        }

        public static Vector3 ToLinearVelocity(Vector3 deltaPosition, float deltaTime)
        {
            return deltaPosition / deltaTime;
        }

        public static Vector3 ToLinearVelocity(Vector3 startPosition, Vector3 destinationPosition, float deltaTime)
        {
            return ToLinearVelocity(destinationPosition - startPosition, deltaTime);
        }

        public static Vector3 ToAngularVelocity(Quaternion deltaRotation, float deltaTime)
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

        public static Vector3 ToAngularVelocity(Quaternion startRotation, Quaternion destinationRotation, float deltaTime)
        {
            return ToAngularVelocity(destinationRotation * Quaternion.Inverse(startRotation), deltaTime);
        }

        public static bool IsValid(this Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)) && !(float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
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
        /// <param name="colliders">A list of colliders representing the object to check</param>
        /// <param name="boneWidth">Optional width of the bone - if not passed, will default to 0</param>
        /// <returns></returns>
        public static bool IsBoneWithinObject(this Leap.Bone bone, List<Collider> colliders, float boneWidth = 0)
        {
            for (int i = 0; i < colliders.Count; i++)
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

        #region Names
        public static string IndexToFinger(int fingerIndex)
        {
            switch (fingerIndex)
            {
                case 0:
                    return "Thumb";
                case 1:
                    return "Index";
                case 2:
                    return "Middle";
                case 3:
                    return "Ring";
                case 4:
                    return "Pinky";
            }
            return "";
        }

        public static string IndexToJoint(int jointIndex)
        {
            switch (jointIndex)
            {
                case 0:
                    return "Proximal";
                case 1:
                    return "Intermediate";
                case 2:
                    return "Distal";
            }
            return "";
        }
        #endregion
    }
}