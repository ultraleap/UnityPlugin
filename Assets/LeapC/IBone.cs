using System;

namespace Leap
{
    public interface IBone
    {
        Bone.BoneType Type { get; }
        bool IsValid { get; }

        Matrix Basis { get; }
        Vector Center { get; }
        Vector Direction { get; }
        
        Vector NextJoint { get; }
        Vector PrevJoint { get; }

        float Width { get; }
        float Length { get; }

        IBone TransformedShallowCopy(ref Matrix trs);
        IBone TransformedCopy(ref Matrix trs);
        bool Equals(Bone other);
        string ToString();
    }
}
