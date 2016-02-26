using System;
namespace Leap
{
    public interface IFrame
    {
        long Id { get; }
        bool IsValid { get; }
        long Timestamp { get; }
        float CurrentFramesPerSecond { get; }
        
        IFinger Finger(int id);
        FingerList Fingers { get; }

        void AddHand(IHand hand);
        IHand Hand(int id);
        HandList Hands { get; }
        
        InteractionBox InteractionBox { get; }
        TrackedQuad TrackedQuad { get; set; }

        Vector Translation(IFrame sinceFrame);
        float TranslationProbability(IFrame sinceFrame);

        float RotationAngle(IFrame sinceFrame);
        float RotationAngle(IFrame sinceFrame, Vector axis);
        Vector RotationAxis(IFrame sinceFrame);
        Matrix RotationMatrix(IFrame sinceFrame);
        float RotationProbability(IFrame sinceFrame);

        float ScaleFactor(IFrame sinceFrame);
        float ScaleProbability(IFrame sinceFrame);

        int SerializeLength { get; }
        byte[] Serialize { get; }
        void Deserialize(byte[] arg);

        IFrame TransformedShallowCopy(ref Matrix trs);
        IFrame TransformedCopy(ref Matrix trs);

        string ToString();
        bool Equals(IFrame other);
    }
}
