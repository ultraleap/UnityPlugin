using UnityEngine;
using System.Collections;

namespace Leap
{
    public class TransformedFrame : IFrame
    {
        private Matrix _trs;
        private IFrame _original;

        HandList _hands = new HandList(3);
        FingerList _fingers = new FingerList(15);

        public TransformedFrame (ref Matrix trs, IFrame original)
        {
            Set(ref trs, original);
        }
        
        public void Set (ref Matrix newTrs, IFrame newOriginal)
        {
            for (int i = 0; i < newOriginal.Hands.Count; i++)
            {
                if (_hands.Count > i)
                {
                    ((TransformedHand)_hands[i]).Set(ref newTrs, newOriginal.Hands[i]);
                    // In this case I don't have to add the fingers again
                    // since the instances will be reused
                }
                else
                {
                    IHand transformedHand = newOriginal.Hands[i].TransformedShallowCopy(ref newTrs);
                    _hands.Add(transformedHand);

                    _fingers.AddRange(transformedHand.Fingers);
                }
            }

            for (int i = newOriginal.Hands.Count; i < _hands.Count; i++)
                _hands.RemoveAt(i);

            _trs = newTrs;
            _original = newOriginal;
        }

        public long Id
        {
            get { return _original.Id; }
        }

        public bool IsValid
        {
            get { return _original.IsValid; }
        }

        public long Timestamp
        {
            get { return _original.Timestamp; }
        }

        public float CurrentFramesPerSecond
        {
            get { return _original.CurrentFramesPerSecond; }
        }

        public IFinger Finger(int id)
        {
            return this.Fingers.Find(delegate(IFinger item)
            {
                return item.Id == id;
            });
        }

        public FingerList Fingers
        {
            get { return _fingers; }
        }

        public void AddHand(IHand hand)
        {
            _hands.Add(hand);
        }

        public IHand Hand(int id)
        {
            return this.Hands.Find(delegate(IHand item)
            {
                return item.Id == id;
            });
        }

        public HandList Hands
        {
            get { return _hands; }
        }

        public InteractionBox InteractionBox
        {
            get {
                // TODO: should InteractionBox be transformed, too?
                return _original.InteractionBox;
            }
        }

        public TrackedQuad TrackedQuad
        {
            get
            {
                return _original.TrackedQuad;
            }
            set
            {
                _original.TrackedQuad = value;
            }
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
            // This is probably wrong. The RotationMatrix method
            // is always returning the identity matrix, which means
            // no rotation in any reference frame. This way, the
            // original code can disregard the LeapMatrix provided
            // in the TransformedCopy method.
            return _original.RotationMatrix(sinceFrame);
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

        public int SerializeLength
        {
            get { throw new System.NotImplementedException(); }
        }

        public byte[] Serialize
        {
            get {
                return _original.TransformedCopy(ref _trs).Serialize;
            }
        }

        public void Deserialize(byte[] arg)
        {
            throw new System.NotImplementedException();
        }

        public IFrame TransformedShallowCopy(ref Matrix trs)
        {
            return new TransformedFrame(ref trs, this);
        }

        public IFrame TransformedCopy(ref Matrix trs)
        {
            // This may be much trickier than it seems.
            // Only enable after automated testing.
            throw new System.NotImplementedException();

            //Frame transformedFrame = new Frame(Id,
            //                             Timestamp,
            //                             CurrentFramesPerSecond,
            //                             new InteractionBox(InteractionBox.Center, InteractionBox.Size));

            ////TODO should InteractionBox be transformed, too?

            //for (int h = 0; h < this.Hands.Count; h++)
            //    transformedFrame.AddHand(this.Hands[h].TransformedCopy(ref trs));

            //return transformedFrame;
        }

        public bool Equals(IFrame other)
        {
            return this.IsValid && other.IsValid && 
                (this.Id == other.Id) && 
                (this.Timestamp == other.Timestamp);
        }
    }
}