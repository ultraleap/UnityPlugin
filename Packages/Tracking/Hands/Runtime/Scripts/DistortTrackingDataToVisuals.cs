/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap;
using Leap.Unity;
using Leap.Unity.HandsModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.HandsModule
{
    /// <summary>
    /// A post process provider that allows hand visuals to generate leap hands that can be used to distort tracking data to visuals
    /// This should be used in cases where you have visual hands that do not conform to leap data size, e.g. a monster hand that has extra long fingers
    /// </summary>
    public class DistortTrackingDataToVisuals : PostProcessProvider
    {
        public HandBinder LeftHand;
        public HandBinder RightHand;

        public float fingerTipScale = 0.8f;

        public override void ProcessFrame(ref Frame inputFrame)
        {
            var hands = new List<Hand>();

            if (LeftHand != null && LeftHand.gameObject.activeInHierarchy)
            {
                var leftLeapHand = inputFrame.GetHand(Chirality.Left);
                if (leftLeapHand != null)
                {
                    leftLeapHand = LeftHand.BoundHand.GenerateLeapHand(leftLeapHand, fingerTipScale);

                    if (leftLeapHand != null)
                    {
                        leftLeapHand.IsLeft = true;
                        hands.Add(leftLeapHand);
                    }
                }
            }

            if (RightHand != null && RightHand.gameObject.activeInHierarchy)
            {
                var rightLeapHand = inputFrame.GetHand(Chirality.Right);
                if (rightLeapHand != null)
                {
                    rightLeapHand = RightHand.BoundHand.GenerateLeapHand(rightLeapHand, fingerTipScale);

                    if (rightLeapHand != null)
                    {
                        rightLeapHand.IsLeft = false;
                        hands.Add(rightLeapHand);
                    }
                }
            }

            if (hands.Count > 0)
            {
                inputFrame.Hands.Clear();
                inputFrame.Hands = hands;
            }
        }
    }
}