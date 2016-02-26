using UnityEngine;
using System.Collections;

namespace Leap
{
    public class TransformedFinger : IFinger
    {
        private Matrix _trs;
        private IFinger _original;
        private TransformedBone[] _bones = new TransformedBone[4];

        public TransformedFinger (ref Matrix trs, IFinger original)
        {
            Set(ref trs, original);
        }

        public int Id
        {
            get { return _original.Id; }
        }

        public int FrameId
        {
            get { return _original.FrameId; }
        }

        public int HandId
        {
            get { return _original.HandId; }
        }

        public float TimeVisible
        {
            get { return _original.TimeVisible; }
        }

        public Finger.FingerType Type
        {
            get { return _original.Type; }
        }

        public bool IsExtended
        {
            get { return _original.IsExtended; }
        }

        public bool IsValid
        {
            get { return _original.IsValid; }
        }

        public float Length
        {
            get {
                float dScale = _trs.zBasis.Magnitude;
                return dScale * _original.Length;
            }
        }

        public float Width
        {
            get {
                float hScale = _trs.xBasis.Magnitude;
                return hScale * _original.Width;
            }
        }

        public Vector Direction
        {
            get { return _trs.TransformDirection(_original.Direction).Normalized; }
        }

        public Vector JointPosition(Finger.FingerJoint jointIx)
        {
            return _trs.TransformPoint(_original.JointPosition(jointIx));
        }

        public Vector StabilizedTipPosition
        {
            get { return _trs.TransformPoint(_original.StabilizedTipPosition); }
        }

        public Vector TipPosition
        {
            get { return _trs.TransformPoint(_original.TipPosition); }
        }

        public Vector TipVelocity
        {
            get { return _trs.TransformPoint(_original.TipVelocity); }
        }

        public IBone Bone(Bone.BoneType boneIx)
        {
            return _bones[(int)boneIx];
        }

        public IFinger TransformedShallowCopy(ref Matrix trs)
        {
            return new TransformedFinger(ref trs, this);
        }

        public IFinger TransformedCopy(ref Matrix trs)
        {
            // This may be much trickier than it seems.
            // Only enable after automated testing.
            throw new System.NotImplementedException();

            //float dScale = trs.zBasis.Magnitude;
            //float hScale = trs.xBasis.Magnitude;

            //return new Finger(FrameId,
            //                  HandId,
            //                  Id,
            //                  TimeVisible,
            //                  trs.TransformPoint(TipPosition),
            //                  trs.TransformPoint(TipVelocity),
            //                  trs.TransformDirection(Direction).Normalized,
            //                  trs.TransformPoint(StabilizedTipPosition),
            //                  Width * hScale,
            //                  Length * dScale,
            //                  IsExtended,
            //                  Type,
            //                  Bone(Leap.Bone.BoneType.TYPE_METACARPAL).TransformedCopy(ref trs),
            //                  Bone(Leap.Bone.BoneType.TYPE_PROXIMAL).TransformedCopy(ref trs),
            //                  Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).TransformedCopy(ref trs),
            //                  Bone(Leap.Bone.BoneType.TYPE_DISTAL).TransformedCopy(ref trs));
        }

        public void Set(ref Matrix newTrs, IFinger finger)
        {
            for (int i = 0; i < 4; i++)
            {
                if (_bones[i] == null)
                {
                    _bones[i] = (TransformedBone) finger.Bone((Bone.BoneType)i).TransformedShallowCopy(ref newTrs);
                }
                else
                {
                    _bones[i].Set(ref newTrs, finger.Bone((Bone.BoneType)i));
                }
            }

            _trs = newTrs;
            _original = finger;
        }
    }
}