using UnityEngine;
using System.Collections;

namespace Leap
{
    public class InvalidFinger : IFinger
    {
        public IBone Bone(Bone.BoneType boneIx)
        {
            throw new System.NotImplementedException();
        }

        public Vector Direction
        {
            get { throw new System.NotImplementedException(); }
        }

        public int HandId
        {
            get { throw new System.NotImplementedException(); }
        }

        public int Id
        {
            get { throw new System.NotImplementedException(); }
        }

        public int FrameId
        {
            get { throw new System.NotImplementedException(); }
        }

        public bool IsExtended
        {
            get { throw new System.NotImplementedException(); }
        }

        public bool IsValid
        {
            get { return false; }
        }

        public Vector JointPosition(Finger.FingerJoint jointIx)
        {
            throw new System.NotImplementedException();
        }

        public float Length
        {
            get { throw new System.NotImplementedException(); }
        }

        public Vector StabilizedTipPosition
        {
            get { throw new System.NotImplementedException(); }
        }

        public float TimeVisible
        {
            get { throw new System.NotImplementedException(); }
        }

        public Vector TipPosition
        {
            get { throw new System.NotImplementedException(); }
        }

        public Vector TipVelocity
        {
            get { throw new System.NotImplementedException(); }
        }

        public IFinger TransformedShallowCopy(ref Matrix trs)
        {
            throw new System.NotImplementedException();
        }

        public IFinger TransformedCopy(ref Matrix trs)
        {
            throw new System.NotImplementedException();
        }

        public Finger.FingerType Type
        {
            get { throw new System.NotImplementedException(); }
        }

        public float Width
        {
            get { throw new System.NotImplementedException(); }
        }
    }
}