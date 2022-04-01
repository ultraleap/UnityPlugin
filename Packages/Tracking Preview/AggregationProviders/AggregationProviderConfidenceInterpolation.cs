/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Encoding;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity
{

    /// <summary>
    /// possible structure to implement our own aggregation code,
    /// gets all hands and lerps between them using confidences (could be extended to treat joints and overall hand pos + rot differently),
    /// Confidences could be calculated as a combination of lots of things (example: relative hand pos)
    /// </summary>
    public class AggregationProviderConfidenceInterpolation : LeapAggregatedProviderBase
    {
        protected override Frame MergeFrames(Frame[] frames)
        {
            List<Hand> leftHands = new List<Hand>();
            List<Hand> rightHands = new List<Hand>();

            List<float> leftHandConfidences = new List<float>();
            List<float> rightHandConfidences = new List<float>();

            // make lists of all left and right hands found in each frame and also make a list of their confidences
            for (int i = 0; i < frames.Length; i++)
            {
                Frame frame = frames[i];
                foreach (Hand hand in frame.Hands)
                {
                    if (hand.IsLeft)
                    {
                        leftHands.Add(hand);
                        leftHandConfidences.Add(CalculateHandConfidence(i, hand));
                    }

                    else
                    {
                        rightHands.Add(hand);
                        rightHandConfidences.Add(CalculateHandConfidence(i, hand));
                    }
                }
            }

            // normalize confidences:
            float sum = leftHandConfidences.Sum();
            for (int i = 0; i < leftHandConfidences.Count; i++)
            {
                leftHandConfidences[i] /= sum;
            }
            sum = rightHandConfidences.Sum();
            for (int i = 0; i < rightHandConfidences.Count; i++)
            {
                rightHandConfidences[i] /= sum;
            }


            // combine hands using their confidences
            // --> could write a function in VectorHand that combines a list of VectorHands using list of confidences to make this more efficient if there are more than two providers
            List<Hand> mergedHands = new List<Hand>();

            if (leftHands.Count > 0)
            {
                VectorHand leftHand = new VectorHand(leftHands[0]);

                for (int i = 1; i < leftHands.Count; i++)
                {
                    float lerpValue = leftHandConfidences.Take(i).Sum() / leftHandConfidences.Take(i + 1).Sum();

                    VectorHand temp = new VectorHand();
                    temp.FillLerped(new VectorHand(leftHands[i]), leftHand, lerpValue);
                    leftHand = temp;
                }

                Hand decodedHand = new Hand();
                leftHand.Decode(decodedHand);
                mergedHands.Add(decodedHand);
            }

            if (rightHands.Count > 0)
            {
                VectorHand rightHand = new VectorHand(rightHands[0]);

                for (int i = 1; i < rightHands.Count; i++)
                {
                    float lerpValue = rightHandConfidences.Take(i).Sum() / rightHandConfidences.Take(i + 1).Sum();

                    VectorHand temp = new VectorHand();
                    temp.FillLerped(new VectorHand(rightHands[i]), rightHand, lerpValue);
                    rightHand = temp;
                }

                Hand decodedHand = new Hand();
                rightHand.Decode(decodedHand);
                mergedHands.Add(decodedHand);
            }

            // get frame data from first frame and add merged hands to it
            Frame mergedFrame = frames[0];
            mergedFrame.Hands = mergedHands;

            return mergedFrame;

        }

        /// <summary>
        /// combine different confidence functions to get an overall confidence for the given hand
        /// uses frame_idx to find the corresponding provider that saw this hand
        /// </summary>
        float CalculateHandConfidence(int frame_idx, Hand hand)
        {
            float confidence = 0;

            confidence = Confidence_RelativeHandPos(providers[frame_idx].transform, hand.PalmPosition.ToVector3());


            return confidence;
        }

        /// <summary>
        /// uses the hand pos relative to the device to calculate a confidence.
        /// using a 2d gauss with bigger spread and smaller amplitude when further away from the camera
        /// </summary>
        float Confidence_RelativeHandPos(Transform deviceOrigin, Vector3 handPos)
        {
            Vector3 relativeHandPos = deviceOrigin.InverseTransformPoint(handPos);

            // 2d gauss

            // amplitude
            float a = 1 - relativeHandPos.y;
            // pos of center of peak
            float x0 = 0;
            float y0 = 0;
            // spread
            float sigmaX = relativeHandPos.y;
            float sigmaY = relativeHandPos.y;

            // input pos
            float x = relativeHandPos.x;
            float y = relativeHandPos.z;

            float confidence = a * Mathf.Exp(-(Mathf.Pow(x - x0, 2) / (2 * Mathf.Pow(sigmaX, 2)) + Mathf.Pow(y - y0, 2) / (2 * Mathf.Pow(sigmaY, 2))));

            if (confidence < 0) confidence = 0;

            return confidence;
        }
    }
}