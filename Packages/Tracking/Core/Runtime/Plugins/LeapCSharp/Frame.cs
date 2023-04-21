/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The Frame class represents a set of hand and finger tracking data detected
    /// in a single frame.
    /// 
    /// The Leap Motion software detects hands, fingers and tools within the tracking area, reporting
    /// their positions, orientations, gestures, and motions in frames at the Leap Motion frame rate.
    /// 
    /// Access Frame objects through an instance of the Controller class.
    /// @since 1.0
    /// </summary>
    [Serializable]
    public class Frame : IEquatable<Frame>
    {
        [ThreadStatic]
        private static Queue<Hand> _handPool;

        /// <summary>
        /// Constructs a Frame object.
        /// 
        /// Frame instances created with this constructor are invalid.
        /// Get valid Frame objects by calling the Controller.Frame() function.
        /// 
        /// The only time you should use this constructor is before deserializing
        /// serialized frame data, or if you are going to be passing this Frame
        /// to a method that fills it with valid data.
        /// 
        /// @since 1.0
        /// </summary>
        public Frame(UInt32 DeviceID = 1)
        {
            Hands = new List<Hand>();
            this.DeviceID = DeviceID;
        }


        /// <summary>
        /// Constructs a new Frame.
        /// @since 3.0
        /// </summary>
        public Frame(long id, long timestamp, float fps, List<Hand> hands)
        {
            Id = id;
            Timestamp = timestamp;
            CurrentFramesPerSecond = fps;
            Hands = hands;
            DeviceID = 1;
        }

        /// <summary>
        /// The Device ID that this frame was seen from.
        /// 
        /// 1-Indexed; Non-Deterministic order
        ///
        /// Only valid when `supportsMultipleDevices` is true on the LeapProvider.
        /// 
        /// @since 4.1
        /// </summary>
        public UInt32 DeviceID;

        /// <summary>
        /// The Hand object with the specified ID in this frame, or null if none
        /// exists.
        /// 
        /// Use the Frame.Hand() function to retrieve the Hand object from 
        /// this frame using an ID value obtained from a previous frame. 
        /// This function always returns a Hand object, but if no hand 
        /// with the specified ID is present, an invalid Hand object is returned. 
        /// 
        /// Note that ID values persist across frames, but only until tracking of a 
        /// particular object is lost. If tracking of a hand is lost and subsequently 
        /// regained, the new Hand object representing that physical hand may have 
        /// a different ID than that representing the physical hand in an earlier frame. 
        /// @since 1.0 </summary>
        public Hand Hand(int id)
        {
            for (int i = Hands.Count; i-- != 0;)
            {
                if (Hands[i].Id == id)
                {
                    return Hands[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Compare Frame object equality.
        /// 
        /// Two Frame objects are equal if and only if both Frame objects represent
        /// the exact same frame of tracking data and both Frame objects are valid.
        /// @since 1.0
        /// </summary>
        public bool Equals(Frame other)
        {
            return Id == other.Id && Timestamp == other.Timestamp;
        }

        /// <summary>
        /// A string containing a brief, human readable description of the Frame object.
        /// @since 1.0
        /// </summary>
        public override string ToString()
        {
            return "Frame id: " + this.Id + " timestamp: " + this.Timestamp;
        }

        /// <summary>
        /// A unique ID for this Frame.
        /// 
        /// Consecutive frames processed by the Leap Motion software have consecutive
        /// increasing values. You can use the frame ID to avoid processing the same
        /// Frame object twice, as well as to make sure that your application processes 
        /// every frame.
        /// 
        /// @since 1.0
        /// </summary>
        public long Id;

        /// <summary>
        /// The frame capture time in microseconds elapsed since an arbitrary point in
        /// time in the past.
        /// 
        /// You can use Controller.Now() to calculate the age of the frame.
        /// 
        /// @since 1.0
        /// </summary>
        public long Timestamp;

        /// <summary>
        /// The instantaneous framerate.
        /// 
        /// The rate at which the Leap Motion software is providing frames of data
        /// (in frames per second). The framerate can fluctuate depending on available computing
        /// resources, activity within the device field of view, software tracking settings,
        /// and other factors.
        /// 
        /// @since 1.0
        /// </summary>
        public float CurrentFramesPerSecond;

        /// <summary>
        /// The list of Hand objects detected in this frame, given in arbitrary order.
        /// The list can be empty if no hands are detected.
        /// 
        /// @since 1.0
        /// </summary>
        public List<Hand> Hands;

        /// <summary>
        /// Resizes the Hand list to have a specific size.  If the size is decreased,
        /// the removed hands are placed into the hand pool.  If the size is increased, the
        /// new spaces are filled with hands taken from the hand pool.  If the pool is
        /// empty, new hands are constructed instead.
        /// </summary>
        public void ResizeHandList(int count)
        {
            if (_handPool == null)
            {
                _handPool = new Queue<Hand>();
            }

            while (Hands.Count < count)
            {
                Hand newHand;
                if (_handPool.Count > 0)
                {
                    newHand = _handPool.Dequeue();
                }
                else
                {
                    newHand = new Hand();
                }
                Hands.Add(newHand);
            }

            while (Hands.Count > count)
            {
                Hand lastHand = Hands[Hands.Count - 1];
                Hands.RemoveAt(Hands.Count - 1);
                _handPool.Enqueue(lastHand);
            }
        }
    }
}