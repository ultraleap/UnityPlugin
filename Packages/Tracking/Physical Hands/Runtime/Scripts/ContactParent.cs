using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.PhysicalHands
{
    public abstract class ContactParent : MonoBehaviour
    {
        private ContactHand _leftHand;
        public ContactHand LeftHand => _leftHand;

        private ContactHand _rightHand;
        public ContactHand RightHand => _rightHand;

        public PhysicalHandsManager _physicalHandsManager;

        public PhysicalHandsManager PhysicalHandsManager => _physicalHandsManager;

        private void Start()
        {
            _physicalHandsManager = GetComponentInParent<PhysicalHandsManager>();
            GenerateHands();

            PhysicalHandsManager.HandsInitiated();
        }

        internal abstract void GenerateHands();

        internal void GenerateHandsObjects(System.Type handType, bool callGenerate = true)
        {
            GameObject handObject = new GameObject($"Left {handType.Name}", handType);
            handObject.transform.parent = transform;
            _leftHand = handObject.GetComponent<ContactHand>();
            _leftHand._handedness = Chirality.Left;
            if (callGenerate)
            {
                _leftHand.GenerateHand();
            }

            handObject = new GameObject($"Right {handType.Name}", handType);
            handObject.transform.parent = transform;
            _rightHand = handObject.GetComponent<ContactHand>();
            _rightHand._handedness = Chirality.Right;
            if (callGenerate)
            {
                _rightHand.GenerateHand();
            }
        }

        internal void UpdateFrame(Frame inputFrame)
        {
            UpdateHand(PhysicalHandsManager._leftHandIndex, _leftHand, inputFrame);
            UpdateHand(PhysicalHandsManager._rightHandIndex, _rightHand, inputFrame);
        }

        private void UpdateHand(int index, ContactHand hand, Frame inputFrame)
        {
            if (hand.IsHandPhysical && !Time.inFixedTimeStep)
            {
                return;
            }

            if (index != -1)
            {
                hand.dataHand = hand.dataHand.CopyFrom(inputFrame.Hands[index]);

                if (!hand.tracked)
                {
                    hand.BeginHand(hand.dataHand);
                }

                // Actually call the update once the hand is ready (sometimes BeginHand sets tracked to true)
                if (hand.tracked)
                {
                    hand.UpdateHand(hand.dataHand);
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
            OutputHand(PhysicalHandsManager._leftHandIndex, _leftHand, ref inputFrame);
            OutputHand(PhysicalHandsManager._rightHandIndex, _rightHand, ref inputFrame);
        }

        private void OutputHand(int index, ContactHand hand, ref Frame inputFrame)
        {
            if (index != -1)
            {
                if (hand.tracked)
                {
                    inputFrame.Hands[index].CopyFrom(hand.OutputHand());
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

        protected virtual void OnValidate()
        {
            if (PhysicalHandsManager == null)
            {
                _physicalHandsManager = GetComponentInParent<PhysicalHandsManager>();
            }
        }
    }
}