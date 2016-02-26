using UnityEngine;
using System.Collections;

namespace Leap
{
    public class InvalidBone : IBone
    {
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

        public IBone TransformedShallowCopy(ref Matrix trs)
        {
            throw new System.NotImplementedException();
        }

        public IBone TransformedCopy(ref Matrix trs)
        {
            throw new System.NotImplementedException();
        }

        public bool Equals(Bone other)
        {
            return other.IsValid == false;
        }
    }
}