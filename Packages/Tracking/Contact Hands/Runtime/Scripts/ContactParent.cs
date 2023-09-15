using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public abstract class ContactParent : MonoBehaviour
    {
        public ContactHand leftHand, rightHand;

        public ContactManager contactManager;

        private void Start()
        {
            contactManager = GetComponentInParent<ContactManager>();
            GenerateHands();
        }

        internal abstract void GenerateHands();

        internal void GenerateHandsObjects(System.Type handType, bool callGenerate = true)
        {
            GameObject handObject = new GameObject($"Left {handType.Name}", handType);
            handObject.transform.parent = transform;
            leftHand = handObject.GetComponent<ContactHand>();
            leftHand.handedness = Chirality.Left;
            if (callGenerate)
            {
                leftHand.GenerateHand();
            }

            handObject = new GameObject($"Right {handType.Name}", handType);
            handObject.transform.parent = transform;
            rightHand = handObject.GetComponent<ContactHand>();
            rightHand.handedness = Chirality.Right;
            if (callGenerate)
            {
                rightHand.GenerateHand();
            }
        }

        internal void UpdateFrame()
        {
            UpdateHand(contactManager._leftHandIndex, leftHand, contactManager._leftDataHand);
            UpdateHand(contactManager._rightHandIndex, rightHand, contactManager._rightDataHand);
        }

        private void UpdateHand(int index, ContactHand hand, Hand dataHand)
        {
            if (index != -1)
            {
                hand.dataHand.CopyFrom(dataHand);
                if (!hand.tracked)
                {
                    hand.BeginHand(dataHand);
                }
                // Actually call the update once the hand is ready
                if (hand.tracked)
                {
                    hand.UpdateHand(dataHand);
                }
            }
            else
            {
                if (hand.tracked || hand.resetting)
                {
                    hand.FinishHand();
                }
            }
        }

        internal void OutputFrame(ref Frame inputFrame)
        {
            contactManager._leftHandIndex = inputFrame.Hands.FindIndex(x => x.IsLeft);
            OutputHand(contactManager._leftHandIndex, leftHand, ref inputFrame);
            contactManager._rightHandIndex = inputFrame.Hands.FindIndex(x => x.IsRight);
            OutputHand(contactManager._rightHandIndex, rightHand, ref inputFrame);
        }

        private void OutputHand(int index, ContactHand hand, ref Frame inputFrame)
        {
            if (index == -1)
            {
                if (hand.tracked)
                {
                    inputFrame.Hands.Add(hand.OutputHand());
                }
            }
            else
            {
                if (hand.tracked)
                {
                    inputFrame.Hands[index].CopyFrom(hand.OutputHand());
                }
                else
                {
                    inputFrame.Hands.RemoveAt(index);
                }
            }
        }

        internal abstract void PostFixedUpdateFrame();

        internal void ProcessHandIntersection()
        {

        }

        internal void ProcessHandOverlaps()
        {

        }

        protected virtual void OnValidate()
        {
            if (contactManager == null)
            {
                contactManager = GetComponentInParent<ContactManager>();
            }
        }
    }
}