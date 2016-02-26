using UnityEngine;
using System.Collections;

namespace Leap
{
    public class InvalidArm : IArm
    {
        public Vector ElbowPosition
        {
            get { throw new System.NotImplementedException(); }
        }

        public Vector WristPosition
        {
            get { throw new System.NotImplementedException(); }
        }

        public IArm TransformedShallowCopy(ref Matrix trs)
        {
            throw new System.NotImplementedException();
        }

        public IArm TransformedCopy(ref Matrix trs)
        {
            throw new System.NotImplementedException();
        }

        public bool Equals(IArm other)
        {
            throw new System.NotImplementedException();
        }

        public Bone.BoneType Type
        {
            get { throw new System.NotImplementedException(); }
        }

        public bool IsValid
        {
            get { return false; }
        }

        public Matrix Basis
        {
            get { throw new System.NotImplementedException(); }
        }

        public Vector Center
        {
            get { throw new System.NotImplementedException(); }
        }

        public Vector Direction
        {
            get { throw new System.NotImplementedException(); }
        }

        public Vector NextJoint
        {
            get { throw new System.NotImplementedException(); }
        }

        public Vector PrevJoint
        {
            get { throw new System.NotImplementedException(); }
        }

        public float Width
        {
            get { throw new System.NotImplementedException(); }
        }

        public float Length
        {
            get { throw new System.NotImplementedException(); }
        }

        IBone IBone.TransformedShallowCopy(ref Matrix trs)
        {
            throw new System.NotImplementedException();
        }

        IBone IBone.TransformedCopy(ref Matrix trs)
        {
            throw new System.NotImplementedException();
        }

        public bool Equals(Bone other)
        {
            return other.IsValid == false;
        }
    }
}