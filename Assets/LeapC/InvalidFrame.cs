using UnityEngine;
using System.Collections;

namespace Leap
{
    public class InvalidFrame : IFrame
    {
        public long Id
        {
            get { throw new System.NotImplementedException(); }
        }

        public bool IsValid
        {
            get { return false; }
        }

        public long Timestamp
        {
            get { throw new System.NotImplementedException(); }
        }

        public float CurrentFramesPerSecond
        {
            get { throw new System.NotImplementedException(); }
        }

        public IFinger Finger(int id)
        {
            throw new System.NotImplementedException();
        }

        public FingerList Fingers
        {
            get { throw new System.NotImplementedException(); }
        }

        public void AddHand(IHand hand)
        {
            throw new System.NotImplementedException();
        }

        public IHand Hand(int id)
        {
            throw new System.NotImplementedException();
        }

        public HandList Hands
        {
            get { throw new System.NotImplementedException(); }
        }

        public InteractionBox InteractionBox
        {
            get { throw new System.NotImplementedException(); }
        }

        public TrackedQuad TrackedQuad
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
                throw new System.NotImplementedException();
            }
        }

        public Vector Translation(IFrame sinceFrame)
        {
            throw new System.NotImplementedException();
        }

        public float TranslationProbability(IFrame sinceFrame)
        {
            throw new System.NotImplementedException();
        }

        public float RotationAngle(IFrame sinceFrame)
        {
            throw new System.NotImplementedException();
        }

        public float RotationAngle(IFrame sinceFrame, Vector axis)
        {
            throw new System.NotImplementedException();
        }

        public Vector RotationAxis(IFrame sinceFrame)
        {
            throw new System.NotImplementedException();
        }

        public Matrix RotationMatrix(IFrame sinceFrame)
        {
            throw new System.NotImplementedException();
        }

        public float RotationProbability(IFrame sinceFrame)
        {
            throw new System.NotImplementedException();
        }

        public float ScaleFactor(IFrame sinceFrame)
        {
            throw new System.NotImplementedException();
        }

        public float ScaleProbability(IFrame sinceFrame)
        {
            throw new System.NotImplementedException();
        }

        public int SerializeLength
        {
            get { throw new System.NotImplementedException(); }
        }

        public byte[] Serialize
        {
            get { throw new System.NotImplementedException(); }
        }

        public void Deserialize(byte[] arg)
        {
            throw new System.NotImplementedException();
        }

        public IFrame TransformedShallowCopy(ref Matrix trs)
        {
            throw new System.NotImplementedException();
        }

        public IFrame TransformedCopy(ref Matrix trs)
        {
            throw new System.NotImplementedException();
        }

        public bool Equals(IFrame other)
        {
            return other.IsValid == false;
        }
    }
}