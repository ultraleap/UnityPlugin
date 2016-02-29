using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Leap
{
    public class TransformedFrame : IFrame
    {
        private Matrix _trs;
        private IFrame _original;

        List<IHand> _hands = new List<IHand>(3);

        public TransformedFrame ()
        {
            Matrix identity = Matrix.Identity;
            Set(ref identity, new Frame());
        }

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
                }
                else
                {
                    IHand transformedHand = newOriginal.Hands[i].TransformedShallowCopy(ref newTrs);
                    _hands.Add(transformedHand);
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

        public long Timestamp
        {
            get { return _original.Timestamp; }
        }

        public float CurrentFramesPerSecond
        {
            get { return _original.CurrentFramesPerSecond; }
        }

        public IHand Hand(int id)
        {
            return this.Hands.Find(delegate(IHand item)
            {
                return item.Id == id;
            });
        }

        public List<IHand> Hands
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

        public int SerializeLength
        {
            get { return 0; }
        }

        public byte[] Serialize
        {
            get {
                return _original.TransformedCopy(ref _trs).Serialize;
            }
        }

        public void Deserialize(byte[] arg)
        {
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
            return 
                (this.Id == other.Id) && 
                (this.Timestamp == other.Timestamp);
        }
    }
}