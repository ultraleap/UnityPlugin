using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualizeJointConfidence : MonoBehaviour
{
    public AggregationProviderConfidenceInterpolation aggregationProvider;
    public CapsuleHand hand;

    int provider_idx;

    // Start is called before the first frame update
    void Start()
    {
        provider_idx = System.Array.IndexOf(aggregationProvider.providers, hand.leapProvider);
        hand.SetIndividualSphereColors = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (hand.GetLeapHand() == null) return;

        Color[] colors = hand.SphereColors;
        colors.Fill(hand.SphereColour);

        float[] confidences = aggregationProvider.CalculateJointConfidence(provider_idx, hand.GetLeapHand());

        for (int i = 0; i < confidences.Length; i++)
        {
            colors[i] = Color.Lerp(Color.black, colors[i], confidences[i]);
        }

        hand.SphereColors = colors;

    }
}