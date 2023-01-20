/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Encoding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity
{
    /// <summary>
    /// John's and Flo's aggregation code. An example of how aggregation could be implemented.
    /// only works for the first two hands it sees (of the same chirality)
    /// </summary>
    public class AggregationProviderAngularInterpolation : LeapAggregatedProviderBase
    {
        private Vector3 tempHandPalmPosition;
        private Vector3 midDevicePointPosition;
        private Vector3 midDevicePointForward;
        private Vector3 midDevicePointUp;

        public float cam1Alpha;
        public float cam2Alpha;

        public float leftAngle;
        public float rightAngle;

        public float maxInterpolationAngle = 60;

        public Transform midpointDevices; //used to calculate relative angle and weight hands accordingly. Transform should face direction that bisects FOV of devices 


        protected override Frame MergeFrames(Frame[] frames)
        {
            Frame mergedFrame = frames[0];
            Hand[] mergedHands = MergeHands(frames);
            mergedFrame.Hands = mergedHands == null ? new List<Hand>() : new List<Hand>(MergeHands(frames));
            return mergedFrame;
        }

        private Hand[] MergeHands(Frame[] frames)
        {
            /* 
             * This function returns one set of hands from multiple Leap Providers, weighing their influence using hands' positions relative to devices.
            */

            if (frames.Length == 0) Debug.Log("frames has a length of 0");
            //sort Left and Right hands (some values may be null since never know how many hands are visible, but we clean it up at the end)
            Hand[] LeftHands = new Hand[providers.Length];
            Hand[] RightHands = new Hand[providers.Length];
            Hand[] mergedHands;
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i].Hands == null) Debug.Log("hand is null in frame " + i);
                //frame_timestamps = new List<long>();
                //frame_timestamps.Add(providers[i].CurrentFrame.Timestamp);
                foreach (Hand tempHand in frames[i].Hands)
                {
                    if (tempHand.IsLeft)
                        LeftHands[i] = tempHand;
                    else
                        RightHands[i] = tempHand;
                }
            }

            //combine hands using relative angle between devices:
            Hand confidentLeft = AngularInterpolate(LeftHands, ref cam1Alpha, ref leftAngle);
            Hand confidentRight = AngularInterpolate(RightHands, ref cam2Alpha, ref rightAngle);

            //clean up and return hand arrays with only valid hands
            #region Clean hand arrays
            if (confidentLeft != null && confidentRight != null)
            {
                mergedHands = new Hand[2];
                mergedHands[0] = confidentLeft;
                mergedHands[1] = confidentRight;
            }
            else if (confidentLeft == null && confidentRight == null)
            {
                mergedHands = null;
            }
            else
            {
                mergedHands = new Hand[1];
                if (confidentLeft != null)
                    mergedHands[0] = confidentLeft;
                else
                    mergedHands[0] = confidentRight;
            }
            #endregion

            return mergedHands;
        }

        public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
        {
            // v1 = average palm position
            // v2 = device midpoint up vector
            // n = device midpoint forward vector

            return Mathf.Atan2(
                Vector3.Dot(n, Vector3.Cross(v1, v2)),
                Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;


        }

        private Hand AngularInterpolate(Hand[] handList, ref float alpha, ref float angle)
        {
            /* 
             * Combines list of hands (of one chiarality) into one Leap.Hand, by weighing the relative angle to the devices
            */

            /*
             * TODO: fix handoff of hand when changing providers, check if chirality changed, etc
             * */

            //find average palm position since we don't exactly know which provider is closest to reality:
            #region PreProcess Raw Hand Data
            Hand tempHand = null;
            int numValidHands = 0;
            foreach (Hand hand in handList)
            {
                if (hand != null && hand.Confidence > 0.98f && hand.TimeVisible > 0.5f) //only use hands with high confidence to avoid when hand is barely in view
                {
                    if (tempHand == null)
                    {
                        tempHand = new Hand();
                        tempHand.CopyFrom(hand);
                    }
                    else
                    {
                        Vector3 nH = hand.PalmPosition;
                        Vector3 tH = tempHand.PalmPosition;
                        tempHand.PalmPosition = new Vector3(aprxAvg(nH.x, tH.x), aprxAvg(nH.y, tH.y), aprxAvg(nH.z, tH.z));
                        //tempHand.PalmPosition = hand.PalmPosition;


                    }
                    numValidHands++;
                }
            }

            if (tempHand != null)
            {
                tempHandPalmPosition = tempHand.PalmPosition;
            }
            #endregion

            if (numValidHands > 0)
            {

                //calculate angle between midpoint between devices(i.e. providers):
                Vector3 devicesMiddle = Vector3.zero;
                Vector3 devicesAvgForward = Vector3.zero;
                for (int i = 0; i < providers.Length; i++)
                {
                    devicesAvgForward += providers[i].transform.forward;
                    devicesMiddle += providers[i].transform.position;
                }
                devicesAvgForward = devicesAvgForward / providers.Length;
                devicesMiddle = devicesMiddle / providers.Length;

                midpointDevices.position = devicesMiddle;
                midpointDevices.forward = devicesAvgForward;

                midDevicePointPosition = midpointDevices.position;
                midDevicePointForward = midpointDevices.forward;
                midDevicePointUp = midpointDevices.up;

                Vector3 angleCalculationHandPosition = tempHand.PalmPosition;

                angle = AngleSigned(angleCalculationHandPosition, midpointDevices.position + midpointDevices.up, midpointDevices.forward);
                //Debug.Log(angle);

                alpha = Mathf.Clamp(angle, -maxInterpolationAngle / 2, maxInterpolationAngle / 2);
                alpha = ((alpha + (maxInterpolationAngle / 2)) / (maxInterpolationAngle)); //normalize to a 0-1 scale

                //Interpolate using alpha:
                Hand interpolateHand = new Hand();

                if (numValidHands == 1)
                {
                    interpolateHand.CopyFrom(tempHand);
                }
                else if (numValidHands > 1) //Note: this implementation only works with first 2 hands:
                {
                    VectorHand vectorInterpolatedHand = new VectorHand();
                    vectorInterpolatedHand.FillLerped(new VectorHand(handList[0]), new VectorHand(handList[1]), alpha);
                    vectorInterpolatedHand.Decode(interpolateHand);
                }

                tempHand = interpolateHand;
            }

            return tempHand;
        }

        private float aprxAvg(float avg, float new_sample)
        {
            /*
             * Utility function of running average
            */

            avg -= avg / 2;
            avg += new_sample / 2;

            return avg;
        }

        public void OnDrawGizmos()
        {
            if (tempHandPalmPosition != null)
            {
                Gizmos.DrawSphere(tempHandPalmPosition, 0.01f);
            }

            midDevicePointForward.Normalize();
            Gizmos.DrawSphere(midDevicePointPosition, 0.01f);
            Gizmos.DrawSphere(midDevicePointForward + midDevicePointPosition, 0.02f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(midDevicePointPosition, midDevicePointForward);

            Gizmos.color = Color.green;
            midDevicePointUp.Normalize();
            Gizmos.DrawSphere(midDevicePointUp, 0.01f);
            Gizmos.DrawSphere(midDevicePointUp + midDevicePointPosition, 0.02f);
            Gizmos.DrawLine(midDevicePointPosition, midDevicePointUp);

            var leftLimit = Quaternion.Euler(0, 0, -maxInterpolationAngle * 0.5f) * midDevicePointUp;
            Gizmos.DrawLine(midDevicePointPosition, leftLimit);

            var rightLimit = Quaternion.Euler(0, 0, maxInterpolationAngle * 0.5f) * midDevicePointUp;
            Gizmos.DrawLine(midDevicePointPosition, rightLimit);
        }
    }
}