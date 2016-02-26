using UnityEngine;
using System.Collections;

namespace Leap
{
    public class InvalidHand : IHand
    {
        public IArm Arm
        {
            get { throw new System.NotImplementedException(); }
        }

        public Matrix Basis
        {
            get { throw new System.NotImplementedException(); }
        }

        public float Confidence
        {
            get { throw new System.NotImplementedException(); }
        }

        public Vector Direction
        {
            get { throw new System.NotImplementedException(); }
        }

        public bool Equals(IHand other)
        {
            return other.IsValid == false;
        }

        public IFinger Finger(int id)
        {
            throw new System.NotImplementedException();
        }

        public FingerList Fingers
        {
            get { throw new System.NotImplementedException(); }
        }

        public int FrameId
        {
            get { throw new System.NotImplementedException(); }
        }

        public float GrabAngle
        {
            get { throw new System.NotImplementedException(); }
        }

        public float GrabStrength
        {
            get { throw new System.NotImplementedException(); }
        }

        public int Id
        {
            get { throw new System.NotImplementedException(); }
        }

        public bool IsLeft
        {
            get { throw new System.NotImplementedException(); }
        }

        public bool IsRight
        {
            get { throw new System.NotImplementedException(); }
        }

        public bool IsValid
        {
            get { return false; }
        }

        public Vector PalmNormal
        {
            get { throw new System.NotImplementedException(); }
        }

        public Vector PalmPosition
        {
            get { throw new System.NotImplementedException(); }
        }

        public Vector PalmVelocity
        {
            get { throw new System.NotImplementedException(); }
        }

        public float PalmWidth
        {
            get { throw new System.NotImplementedException(); }
        }

        public float PinchDistance
        {
            get { throw new System.NotImplementedException(); }
        }

        public float PinchStrength
        {
            get { throw new System.NotImplementedException(); }
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

        public Vector SphereCenter
        {
            get { throw new System.NotImplementedException(); }
        }

        public float SphereRadius
        {
            get { throw new System.NotImplementedException(); }
        }

        public Vector StabilizedPalmPosition
        {
            get { throw new System.NotImplementedException(); }
        }

        public float TimeVisible
        {
            get { throw new System.NotImplementedException(); }
        }

        public IHand TransformedShallowCopy(ref Matrix trs)
        {
            throw new System.NotImplementedException();
        }

        public IHand TransformedCopy(ref Matrix trs)
        {
            throw new System.NotImplementedException();
        }

        public Vector Translation(IFrame sinceFrame)
        {
            throw new System.NotImplementedException();
        }

        public float TranslationProbability(IFrame sinceFrame)
        {
            throw new System.NotImplementedException();
        }

        public Vector WristPosition
        {
            get { throw new System.NotImplementedException(); }
        }
    }
}
