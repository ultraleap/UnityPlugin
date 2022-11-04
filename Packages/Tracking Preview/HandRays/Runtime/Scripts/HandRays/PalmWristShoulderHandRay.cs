using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
    public class PalmWristShoulderHandRay : WristShoulderHandRay
    {
        private Vector3 palmWristOffset = new Vector3(0,0.2f,0);

        protected override Vector3 CalculateVisualAimPosition()
        {
            return handRayDirection.Hand.PalmPosition;
        }

        protected override Vector3 CalculateAimPosition()
        {
            return aimPositionFilter.Filter(handRayDirection.Hand.PalmPosition, Time.time);
        }


        protected override Vector3 GetWristOffsetPosition(Hand hand)
        {
            Vector3 localWristPosition = palmWristOffset;
            if (hand.IsRight)
            {
                localWristPosition.x = -localWristPosition.x;
            }

            transformHelper.transform.position = hand.WristPosition;
            transformHelper.transform.rotation = hand.Rotation;
            return transformHelper.TransformPoint(localWristPosition);
        }
    }
}