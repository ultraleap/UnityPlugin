using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public abstract class ContactParent : MonoBehaviour
    {
        public ContactHand leftHand, rightHand;

        public ContactManager contactManager;

        private void Awake()
        {
            contactManager = GetComponentInParent<ContactManager>();
            GenerateHands();
        }

        internal abstract void GenerateHands();

        internal void UpdateFrame()
        {            
            if(contactManager._leftHandIndex != -1)
            {
                leftHand.UpdateHand(contactManager._leftDataHand);
            }

            if(contactManager._rightHandIndex != -1)
            {
                rightHand.UpdateHand(contactManager._rightDataHand);
            }
        }

        internal abstract void PostFixedUpdateFrame();

        internal void ProcessHandIntersection()
        {

        }

        internal void ProcessHandOverlaps()
        {

        }
    }
}