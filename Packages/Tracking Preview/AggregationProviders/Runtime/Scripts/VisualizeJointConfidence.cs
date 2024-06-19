using Ultraleap;
using UnityEngine;

public class VisualizeJointConfidence : MonoBehaviour
{
    public AggregationProviderConfidenceInterpolation aggregationProvider;
    public CapsuleHand hand;

    private int provider_idx;

    // Start is called before the first frame update
    private void Start()
    {
        provider_idx = System.Array.IndexOf(aggregationProvider.providers, hand.leapProvider);
        hand.SetIndividualSphereColors = true;
    }

    // Update is called once per frame
    private void Update()
    {
        if (hand.GetLeapHand() == null)
        {
            return;
        }

        Color[] colors = hand.SphereColors;
        Ultraleap.Utils.Fill(colors, hand.SphereColour);

        float[] confidences = aggregationProvider.CalculateJointConfidence(provider_idx, hand.GetLeapHand());

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                int key = (i * 5) + j + 1;
                int capsuleHandKey = (i * 4) + j;

                colors[capsuleHandKey] = Color.Lerp(Color.black, colors[key], confidences[key]);
            }
        }

        hand.SphereColors = colors;
    }
}