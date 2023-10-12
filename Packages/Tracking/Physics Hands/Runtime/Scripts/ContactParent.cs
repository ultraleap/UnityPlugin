using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.PhysicsHands
{
    public abstract class ContactParent : MonoBehaviour
    {
        private ContactHand _leftHand;
        public ContactHand LeftHand => _leftHand;

        private ContactHand _rightHand;
        public ContactHand RightHand => _rightHand;

        public PhysicsHandsManager physicsHandsManager;

        private void Start()
        {
            physicsHandsManager = GetComponentInParent<PhysicsHandsManager>();
            GenerateHands();
        }

        internal abstract void GenerateHands();

        internal void GenerateHandsObjects(System.Type handType, bool callGenerate = true)
        {
            GameObject handObject = new GameObject($"Left {handType.Name}", handType);
            handObject.transform.parent = transform;
            _leftHand = handObject.GetComponent<ContactHand>();
            _leftHand.handedness = Chirality.Left;
            if (callGenerate)
            {
                _leftHand.GenerateHand();
            }

            handObject = new GameObject($"Right {handType.Name}", handType);
            handObject.transform.parent = transform;
            _rightHand = handObject.GetComponent<ContactHand>();
            _rightHand.handedness = Chirality.Right;
            if (callGenerate)
            {
                _rightHand.GenerateHand();
            }
        }

        internal void UpdateFrame()
        {
            UpdateHand(physicsHandsManager._leftHandIndex, _leftHand, physicsHandsManager._leftDataHand);
            UpdateHand(physicsHandsManager._rightHandIndex, _rightHand, physicsHandsManager._rightDataHand);
        }

        private void UpdateHand(int index, ContactHand hand, Hand dataHand)
        {
            if (hand.IsHandPhysical && !Time.inFixedTimeStep)
            {
                return;
            }
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
            physicsHandsManager._leftHandIndex = inputFrame.Hands.FindIndex(x => x.IsLeft);
            OutputHand(physicsHandsManager._leftHandIndex, _leftHand, ref inputFrame);
            physicsHandsManager._rightHandIndex = inputFrame.Hands.FindIndex(x => x.IsRight);
            OutputHand(physicsHandsManager._rightHandIndex, _rightHand, ref inputFrame);
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
                else if(inputFrame.Hands.Count > 0)
                {
                    inputFrame.Hands.RemoveAt(index);
                }
            }
        }

        internal void PostFixedUpdateFrame()
        {
            PostFixedUpdateFrameLogic();
            _leftHand.PostFixedUpdateHand();
            _rightHand.PostFixedUpdateHand();
        }

        /// <summary>
        /// Happens before the hands update is called.
        /// </summary>
        internal abstract void PostFixedUpdateFrameLogic();

        internal void ProcessHandIntersection()
        {

        }

        internal void ProcessHandOverlaps()
        {

        }

        protected virtual void OnValidate()
        {
            if (physicsHandsManager == null)
            {
                physicsHandsManager = GetComponentInParent<PhysicsHandsManager>();
            }
        }
    }
}