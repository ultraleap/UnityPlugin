using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Leap
{
    public class TransformedHand : IHand
    {
        private IHand _original;
        private Matrix _trs;

        private List<IFinger> _fingers = new List<IFinger>(5);
        private TransformedArm _arm;
        
        public TransformedHand(ref Matrix trs, IHand original)
        {
            Set(ref trs, original);
        }

        public IArm Arm
        {
            get { return _arm; }
        }

        public List<IFinger> Fingers
        {
            get { return _fingers; }
        }

        public int Id
        {
            get { return _original.Id; }
        }

        public long FrameId
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
            return  (this.Id == other.Id) &&
                (this.FrameId == other.FrameId);
        }

        public IFinger Finger(int id)
        {
            return this.Fingers.Find(delegate(IFinger item)
            {
                return item.Id == id;
            });
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

            // Can a hand lose a finger?
            for (int i = newOriginal.Fingers.Count; i < _fingers.Count; i++)
                _fingers.RemoveAt(i);

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