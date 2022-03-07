using Leap;
using Leap.Unity;
using Leap.Unity.HandsModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        if (LeftHand != null)
        {
            var lHand = LeftHand.BoundHand.GenerateLeapHand(LeftHand.LeapHand, fingerTipScale);

            if (lHand != null)
            {
                lHand.IsLeft = true;
                hands.Add(lHand);
            }
        }

        if (RightHand != null)
        {
            var rHand = RightHand.BoundHand.GenerateLeapHand(RightHand.LeapHand, fingerTipScale);

            if (rHand != null)
            {
                rHand.IsLeft = false;
                hands.Add(rHand);
            }
        }

        inputFrame.Hands.Clear();
        inputFrame.Hands = hands;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            passthroughOnly = !passthroughOnly;
        }
    }

}
