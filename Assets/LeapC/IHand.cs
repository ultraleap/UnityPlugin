using System;
namespace Leap
{
    public interface IHand
    {
        IArm Arm { get; }
        FingerList Fingers { get; }

        int Id { get; }        
        int FrameId { get; }
        float TimeVisible { get; }
        float Confidence { get; }

        float GrabAngle { get; }
        float GrabStrength { get; }
        
        bool IsLeft { get; }
        bool IsRight { get; }
        bool IsValid { get; }

        Matrix Basis { get; }
        Vector Direction { get; }

        float PalmWidth { get; }
        Vector PalmNormal { get; }
        Vector PalmPosition { get; }
        Vector PalmVelocity { get; }
        Vector StabilizedPalmPosition { get; }

        Vector WristPosition { get; }

        float PinchDistance { get; }
        float PinchStrength { get; }

        [Obsolete]
        Vector SphereCenter { get; }
        [Obsolete]
        float SphereRadius { get; }
        
        Vector Translation(IFrame sinceFrame);
        float TranslationProbability(IFrame sinceFrame);

        float RotationAngle(IFrame sinceFrame);
        float RotationAngle(IFrame sinceFrame, Vector axis);
        Vector RotationAxis(IFrame sinceFrame);
        Matrix RotationMatrix(IFrame sinceFrame);
        float RotationProbability(IFrame sinceFrame);

        float ScaleFactor(IFrame sinceFrame);
        float ScaleProbability(IFrame sinceFrame);

        IFinger Finger(int id);
        IHand TransformedShallowCopy(ref Matrix trs);
        IHand TransformedCopy(ref Matrix trs);
        
        string ToString();
        bool Equals(IHand other);        
    }
}
