using UnityEngine;
using System.Collections;

namespace Leap
{
    public class TransformedHand : IHand
    {
        private IHand _original;
        private Matrix _trs;

        private FingerList _fingers = new FingerList(5);
        private TransformedArm _arm;
        
        public TransformedHand(ref Matrix trs, IHand original)
        {
            Set(ref trs, original);
        }

        public IArm Arm
        {
            get { return _arm; }
        }

        public FingerList Fingers
        {
            get { return _fingers; }
        }

        public int Id
        {
            get { return _original.Id; }
        }

        public int FrameId
        {
            get { return _original.FrameId; }
        }

        public float TimeVisible
        {
            get { return _original.TimeVisible; }
        }

        public float Confidence
        {
            get { return _original.Confidence; }
        }

        public float GrabAngle
        {
            get { return _original.GrabAngle; }
        }

        public float GrabStrength
        {
            get { return _original.GrabStrength; }
        }

        public bool IsLeft
        {
            get { return _original.IsLeft; }
        }

        public bool IsRight
        {
            get { return _original.IsRight; }
        }

        public bool IsValid
        {
            get { return _original.IsValid; }
        }

        public Matrix Basis
        {
            get {
                Matrix originalBasis = _original.Basis;
                Matrix ret = new Matrix(
                    _trs.TransformDirection(originalBasis.xBasis).Normalized,
                    _trs.TransformDirection(originalBasis.yBasis).Normalized,
                    _trs.TransformDirection(originalBasis.zBasis).Normalized
                    );

                return ret;
                // return _trs * _original.Basis; 
            }
        }

        public Vector Direction
        {
            get { return _trs.TransformDirection(_original.Direction).Normalized; }
        }

        public float PalmWidth
        {
            get {
                float hScale = _trs.xBasis.Magnitude;
                return hScale * _original.PalmWidth;
            }
        }

        public Vector PalmNormal
        {
            get { return _trs.TransformDirection(_original.PalmNormal).Normalized; }
        }

        public Vector PalmPosition
        {
            get { return _trs.TransformPoint(_original.PalmPosition); }
        }

        public Vector PalmVelocity
        {
            get { return _trs.TransformPoint(_original.PalmVelocity); }
        }

        public Vector StabilizedPalmPosition
        {
            get { return _trs.TransformPoint(_original.StabilizedPalmPosition); }
        }

        public Vector WristPosition
        {
            get { return _trs.TransformPoint(_original.WristPosition); }
        }

        public float PinchDistance
        {
            get { return _original.PinchDistance; }
        }

        public float PinchStrength
        {
            get { return _original.PinchStrength;  }
        }

        public Vector SphereCenter
        {
            get { return _trs.TransformPoint(_original.SphereCenter); }
        }

        public float SphereRadius
        {
            get { return _original.SphereRadius; }
        }

        public Vector Translation(IFrame sinceFrame)
        {
            return _trs.TransformDirection(_original.Translation(sinceFrame));
        }

        public float TranslationProbability(IFrame sinceFrame)
        {
            return _original.TranslationProbability(sinceFrame);
        }

        public float RotationAngle(IFrame sinceFrame)
        {
            return _original.RotationAngle(sinceFrame);
        }

        public float RotationAngle(IFrame sinceFrame, Vector axis)
        {
            return _original.RotationAngle(sinceFrame, _trs.RigidInverse().TransformDirection( axis ).Normalized);
        }

        public Vector RotationAxis(IFrame sinceFrame)
        {
            return _trs.TransformDirection(_original.RotationAxis(sinceFrame)).Normalized;
        }

        public Matrix RotationMatrix(IFrame sinceFrame)
        {
            IHand sinceHand = sinceFrame.Hand(this.Id);

            if (!sinceHand.IsValid)
                return Matrix.Identity;

            return this.Basis * sinceHand.Basis.RigidInverse();
        }

        public float RotationProbability(IFrame sinceFrame)
        {
            return _original.RotationProbability(sinceFrame);
        }

        public float ScaleFactor(IFrame sinceFrame)
        {
            return _original.ScaleFactor(sinceFrame);
        }

        public float ScaleProbability(IFrame sinceFrame)
        {
            return _original.ScaleProbability(sinceFrame);
        }

        public IFinger Finger(int id)
        {
            return this.Fingers.Find(delegate(IFinger item)
            {
                return item.Id == id;
            });
        }

        public IHand TransformedShallowCopy(ref Matrix trs)
        {
            return new TransformedHand(ref trs, this);
        }

        public IHand TransformedCopy(ref Matrix trs)
        {
            // This may be much trickier than it seems.
            // Only enable after automated testing.
            throw new System.NotImplementedException();

            //FingerList transformedFingers = new FingerList(5);
            //for (int f = 0; f < this.Fingers.Count; f++)
            //    transformedFingers.Add(Fingers[f].TransformedCopy(ref trs));

            //float hScale = trs.xBasis.Magnitude;
            //return new Hand(FrameId,
            //    Id,
            //    Confidence,
            //    GrabStrength,
            //    GrabAngle,
            //    PinchStrength,
            //    PinchDistance,
            //    PalmWidth * hScale,
            //    IsLeft,
            //    TimeVisible,
            //    Arm.TransformedCopy(ref trs),
            //    transformedFingers,
            //    trs.TransformPoint(PalmPosition),
            //    trs.TransformPoint(StabilizedPalmPosition),
            //    trs.TransformPoint(PalmVelocity),
            //    trs.TransformDirection(PalmNormal).Normalized,
            //    trs.TransformDirection(Direction).Normalized,
            //    trs.TransformPoint(WristPosition));
        }

        public bool Equals(IHand other)
        {
            return this.IsValid &&
                other.IsValid &&
                (this.Id == other.Id) &&
                (this.FrameId == other.FrameId);
        }

        public void Set(ref Matrix newTrs, IHand newOriginal)
        {
            for (int i = 0; i < newOriginal.Fingers.Count; i++)
            {
                if (_fingers.Count > i)
                {
                    ((TransformedFinger)_fingers[i]).Set(ref newTrs, newOriginal.Fingers[i]);
                }
                else
                {
                    _fingers.Add(newOriginal.Fingers[i].TransformedShallowCopy(ref newTrs));
                }
            }

            if (_arm != null)
            {
                _arm.Set(ref newTrs, newOriginal.Arm);
            }
            else
            { 
                _arm = (TransformedArm) newOriginal.Arm.TransformedShallowCopy(ref newTrs);
            }

            _trs = newTrs;
            _original = newOriginal;
        }
    }
}