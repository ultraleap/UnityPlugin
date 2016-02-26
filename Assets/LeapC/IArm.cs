using System;
namespace Leap
{
    public interface IArm : IBone
    {
        Vector ElbowPosition { get; }
        Vector WristPosition { get; }

        new IArm TransformedShallowCopy(ref Matrix trs);
        new IArm TransformedCopy(ref Matrix trs);

        bool Equals(IArm other);
        new string ToString();
    }
}
