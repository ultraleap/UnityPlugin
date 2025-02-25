using Leap;

public class MetaCompatibilityProvider : PostProcessProvider
{
    public MetaCompatibility.ConversionType ConversionDirection = MetaCompatibility.ConversionType.MetaToLeap;
    
    public override void ProcessFrame(ref Frame inputFrame)
    {
        foreach (var inputHand in inputFrame.Hands)
        {
            switch (ConversionDirection)
            {
                case MetaCompatibility.ConversionType.MetaToLeap:
                    inputHand.FromMetaLayout();
                    break;
                case MetaCompatibility.ConversionType.LeapToMeta:
                    inputHand.ToMetaLayout();
                    break;
            }
        }
    }
}
