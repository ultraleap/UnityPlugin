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