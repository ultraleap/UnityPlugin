using UnityEngine;
using System.Collections;

namespace Leap
{
    public class TransformedArm : TransformedBone, IArm
    {
        private Matrix _trs;
        private IArm _arm;

        public TransformedArm(ref Matrix trs, IArm original) : base(ref trs, original)
        {
            _arm = original;
        }

        public void Set(ref Matrix trs, IArm original)
        {
            _trs = trs;
            _arm = original;

            base.Set(ref trs, original);
        }

        public Vector ElbowPosition
        {
            get { return _trs.TransformPoint(_arm.ElbowPosition); }
        }

        public Vector WristPosition
        {
            get {
                Vector pos = _arm.WristPosition;
                return _trs.TransformPoint(pos); 
            }
        }

        new public IArm TransformedShallowCopy(ref Matrix trs)
        {
            return new TransformedArm(ref trs, this);
        }

        new public IArm TransformedCopy(ref Matrix trs)
        {
            // This may be much trickier than it seems.
            // Only enable after automated testing.
            throw new System.NotImplementedException();

            //float dScale = trs.zBasis.Magnitude;
            //float hScale = trs.xBasis.Magnitude;

            //return new Arm(trs.TransformPoint(PrevJoint),
            //    trs.TransformPoint(NextJoint),
            //    trs.TransformPoint(Center),
            //    trs.TransformDirection(Direction),
            //    Length * dScale,
            //    Width * hScale,
            //    Type,
            //    trs * Basis);
        }

        public bool Equals(IArm other)
        {
            return base.Equals(other as IBone);
        }
    }
}