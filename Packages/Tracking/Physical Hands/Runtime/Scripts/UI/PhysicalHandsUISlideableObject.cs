using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Leap.Unity.PhysicalHands
{
    /// <summary>
    /// This clas is purely to get events from the slideable object.
    /// You do not need to add this class to your object, it will be added automatically by the slider.
    /// </summary>
    internal class PhysicalHandsUISliderHelper : MonoBehaviour, IPhysicalHandGrab, IPhysicalHandContact
    {
        internal Action<ContactHand> _onHandGrab;
        internal Action<ContactHand> _onHandGrabExit;
        internal Action<ContactHand> _onHandContact;
        internal Action<ContactHand> _onHandContactExit;

        public void OnHandContact(ContactHand hand)
        {
            _onHandContact.Invoke(hand);
        }

        public void OnHandContactExit(ContactHand hand)
        {
            _onHandContactExit.Invoke(hand);
        }

        public void OnHandGrab(ContactHand hand)
        {
            _onHandGrab.Invoke(hand);
        }

        public void OnHandGrabExit(ContactHand hand)
        {
            _onHandGrabExit.Invoke(hand);
        }
    }
}
