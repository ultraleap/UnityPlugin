using UnityEngine;
using System.Collections;

namespace Leap
{
    public class TransformedBone : IBone
    {
        private IBone _original;
        private Matrix _trs;

        public TransformedBone(ref Matrix trs, IBone original)
        {
            Set(ref trs, original);
        }

        public Bone.BoneType Type
        {
            get { return _original.Type; }
        }

        public Matrix Basis
        {
            get { return _trs * _original.Basis; }
        }

        public Vector Center
        {
            get { return _trs.TransformPoint( _original.Center ); }
        }

        public Vector Direction
        {
            get { return _trs.TransformDirection(_original.Direction).Normalized; }
        }

        public Vector NextJoint
        {
            get { return _trs.TransformPoint( _original.NextJoint ); }
        }

        public Vector PrevJoint
        {
            get { return _trs.TransformPoint( _original.PrevJoint ); }
        }

        public float Width
        {
            get {
                float hScale = _trs.xBasis.Magnitude;

                return hScale * _original.Width;
            }
        }

        public float Length
        {
            get {
                float dScale = _trs.zBasis.Magnitude;

                return dScale * _original.Length;
            }
        }

        public IBone TransformedShallowCopy(ref Matrix trs)
        {
            return new TransformedBone(ref trs, this);
        }

        public IBone TransformedCopy(ref Matrix trs)
        {
            // This may be much trickier than it seems.
            // Only enable after automated testing.
            throw new System.NotImplementedException();

            //float dScale = trs.zBasis.Magnitude;
            //float hScale = trs.xBasis.Magnitude;

            //return new Bone(
            //    trs.TransformPoint(PrevJoint),
            //    trs.TransformPoint(NextJoint),
            //    trs.TransformPoint(Center),
            //    trs.TransformDirection(Direction).Normalized,
            //    Length * dScale,
            //    Width * hScale,
            //    Type,
            //    trs * Basis);
        }

        public bool Equals(IBone other)
        {
            return (this.Center == other.Center) &&
                   (this.Direction == other.Direction) &&
                   (this.Length == other.Length);
        }

        public void Set(ref Matrix newTrs, IBone bone)
        {
            _trs = newTrs;
            _original = bone;

            if (bone == null)
                Debug.Log("porra");
        }
    }
}
