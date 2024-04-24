using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Leap.Unity.PhysicalHands
{
    /// <summary>
    /// This class is purely to get events from the slideable object.
    /// You do not need to add this class to your object, it will be added automatically by the slider.
    /// </summary>
    public class PhysicalHandsSlideHelper : MonoBehaviour, IPhysicalHandGrab, IPhysicalHandContact
    {
        public Action<ContactHand> _onHandGrab;
        public Action<ContactHand> _onHandGrabExit;
        public Action<ContactHand> _onHandContact;
        public Action<ContactHand> _onHandContactExit;

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
