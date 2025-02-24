using Leap;

public static class MetaCompatibility
{
    public static Hand ToMetaLayout(this Hand hand)
    {
        // This performs the same steps as that were implemented in the OpenXR API layer's code.

        // We need to perform an operation on all of the metacarpals.
        foreach (var finger in hand.fingers)
        {
            if (finger.Type == Finger.FingerType.THUMB) P
            {

            }
            else
            {

            }
        }
    }

    public static Hand FromMetaLayout(this Hand hand)
    {

    }
}
