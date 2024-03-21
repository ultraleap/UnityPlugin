/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap.Unity.PhysicalHands
{
    /// <summary>
    /// Reports the hover event, called when a grab helper is created for the hand.
    /// This is a relatively low accuracy hover.
    /// </summary>
    public interface IPhysicalHandHover
    {
        void OnHandHover(ContactHand hand);
        void OnHandHoverExit(ContactHand hand);
    }

    /// <summary>
    /// Reports the contact event when any part of the hand is in contact with an interactable rigidbody.
    /// This will be called dependant on the contact distance set by the hand.
    /// </summary>
    public interface IPhysicalHandContact
    {
        void OnHandContact(ContactHand hand);
        void OnHandContactExit(ContactHand hand);
    }

    /// <summary>
    /// Reports the grab event when a grab helper starts or stops grabbing a rigidbody.
    /// </summary>
    public interface IPhysicalHandGrab
    {
        void OnHandGrab(ContactHand hand);
        void OnHandGrabExit(ContactHand hand);
    }
}