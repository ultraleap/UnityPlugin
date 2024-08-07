/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap
{

    using TestHandPose = TestHandFactory.TestHandPose;

    /// <summary>
    /// Provides Frame object data to the Unity application by firing events as soon
    /// as Frame data is available. Frames contain all currently tracked Hands in view
    /// of the Leap Motion Controller.
    /// 
    /// LeapProvider defines the basic interface our plugin expects to use to retrieve 
    /// Frame data. This abstraction allows you to create your own LeapProviders, which 
    /// is useful when testing or developing in a context where Ultraleap Hand Tracking 
    /// hardware isn't immediately available.
    /// </summary>
    public abstract class LeapProvider : MonoBehaviour
    {
        public TestHandPose editTimePose = TestHandPose.HeadMountedB;

        public event Action<Frame> OnUpdateFrame;
        public event Action<Frame> OnFixedFrame;
        public event Action<Frame> OnPostUpdateFrame;

        public event Action<Chirality> OnHandFound;
        public event Action<Chirality> OnHandLost;

        /// <summary>
        /// The current frame for this update cycle, in world space. 
        /// 
        /// IMPORTANT!  This frame might be mutable!  If you hold onto a reference
        /// to this frame, or a reference to any object that is a part of this frame,
        /// it might change unexpectedly.  If you want to save a reference, make sure
        /// to make a copy.
        /// </summary>
        public abstract Frame CurrentFrame { get; }

        /// <summary>
        /// The current frame for this fixed update cycle, in world space.
        /// 
        /// IMPORTANT!  This frame might be mutable!  If you hold onto a reference
        /// to this frame, or a reference to any object that is a part of this frame,
        /// it might change unexpectedly.  If you want to save a reference, make sure
        /// to make a copy.
        /// </summary>
        public abstract Frame CurrentFixedFrame { get; }

        /// <summary>
        /// The current tracked state of the left hand. See also OnHandFound and OnHandLost events.
        /// </summary>
        public bool LeftHandTracked { get; private set; }
        /// <summary>
        /// The current tracked state of the right hand. See also OnHandFound and OnHandLost events.
        /// </summary>
        public bool RightHandTracked { get; private set; }

        protected TrackingSource _trackingSource;

        /// <summary>
        /// Represents the source of tracking data
        /// </summary>
        public virtual TrackingSource TrackingDataSource { get { return _trackingSource; } }

        protected void DispatchUpdateFrameEvent(Frame frame)
        {
            if (OnUpdateFrame != null)
            {
                OnUpdateFrame(frame);
            }
            if (OnPostUpdateFrame != null)
            {
                OnPostUpdateFrame(frame);
            }

            DispatchLostAndFoundEvents(frame);
        }

        protected void DispatchFixedFrameEvent(Frame frame)
        {
            if (OnFixedFrame != null)
            {
                OnFixedFrame(frame);
            }

            DispatchLostAndFoundEvents(frame);
        }

        protected void DispatchLostAndFoundEvents(Frame frame)
        {
            bool leftHandTracked = LeftHandTracked;
            HandleHandLostFound(frame, Chirality.Left, ref leftHandTracked);
            LeftHandTracked = leftHandTracked;

            bool rightHandTracked = RightHandTracked;
            HandleHandLostFound(frame, Chirality.Right, ref rightHandTracked);
            RightHandTracked = rightHandTracked;
        }

        private void HandleHandLostFound(Frame frame, Chirality chirality, ref bool tracked)
        {
            Hand hand = frame.GetHand(chirality);

            if (hand != null)
            {
                if (!tracked)
                {
                    tracked = true;
                    OnHandFound?.Invoke(chirality);
                }
            }
            else
            {
                if (tracked)
                {
                    tracked = false;
                    OnHandLost?.Invoke(chirality);
                }
            }
        }
    }

    /// <summary>
    /// Used to determine the source of the tracking data.
    /// NONE - Either not available or has not been detemined yet
    /// LEAPC - A direct connection to the Leap Service via LeapC
    /// OPENXR - An OpenXR connection, most likely not Ultraleap Tracking
    /// OPENXR_LEAP - An OpenXR connection, Ultraleap OpenXR layer is also active
    /// </summary>
    public enum TrackingSource
    {
        NONE,
        LEAPC,
        OPENXR,
        OPENXR_LEAP
    }

    public static class LeapProviderExtensions
    {

        public static Leap.Hand MakeTestHand(this LeapProvider provider, bool isLeft)
        {
            return TestHandFactory.MakeTestHand(isLeft, provider.editTimePose)
                                  .Transform(new LeapTransform(provider.transform));
        }

    }
}