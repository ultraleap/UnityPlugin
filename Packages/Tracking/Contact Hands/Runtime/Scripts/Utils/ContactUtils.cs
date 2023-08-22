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