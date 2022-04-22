using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

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
        Color[] colors = hand.SphereColors;

        float[] confidences = aggregationProvider.CalculateJointConfidence(provider_idx, hand.GetLeapHand());

        for(int i = 0; i < confidences.Length; i++)
        {
            colors[i].a = confidences[i];
        }

        hand.SphereColors = colors;

    }
}
