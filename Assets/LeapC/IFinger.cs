using System;

namespace Leap
{
    public interface IFinger
    {
        int Id { get; }
        int FrameId { get; }
        int HandId { get; }
        float TimeVisible { get; }

        Finger.FingerType Type { get; }
        bool IsExtended { get; }
        bool IsValid { get; }

        float Length { get; }
        float Width { get; }

        Vector Direction { get; }
        Vector JointPosition(Finger.FingerJoint jointIx);
        Vector StabilizedTipPosition { get; }
        
        Vector TipPosition { get; }
        Vector TipVelocity { get; }

        IBone Bone(Bone.BoneType boneIx);
        IFinger TransformedShallowCopy(ref Matrix trs);
        IFinger TransformedCopy(ref Matrix trs);

        string ToString();
    }
}
