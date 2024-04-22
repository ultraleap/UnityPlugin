/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.PhysicalHands
{
    public abstract class ContactParent : MonoBehaviour
    {
        private ContactHand _leftHand;
        public ContactHand LeftHand => _leftHand;

        private ContactHand _rightHand;
        public ContactHand RightHand => _rightHand;

        public PhysicalHandsManager physicalHandsManager;

        private void Start()
        {
            Initialize();
        }

        internal void Initialize()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            physicalHandsManager = GetComponentInParent<PhysicalHandsManager>();

            if (physicalHandsManager != null)
            {
                GenerateHands();
                physicalHandsManager.HandsInitiated();
            }
            else
            {
                Debug.LogWarning("No PhysicalHandsManager has been assigned to the ContactParent. Physical Hands will not work as the ContactParent can not be initialized");
            }
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
            UpdateHand(physicalHandsManager._leftHandIndex, _leftHand, inputFrame);
            UpdateHand(physicalHandsManager._rightHandIndex, _rightHand, inputFrame);
        }

        private void UpdateHand(int index, ContactHand hand, Frame inputFrame)
        {
            if (hand.isHandPhysical && !Time.inFixedTimeStep)
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
            OutputHand(physicalHandsManager._leftHandIndex, _leftHand, ref inputFrame);
            OutputHand(physicalHandsManager._rightHandIndex, _rightHand, ref inputFrame);
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
            _leftHand.PostFixedUpdateHand();
            _rightHand.PostFixedUpdateHand();
        }

        protected virtual void OnValidate()
        {
            if (physicalHandsManager == null)
            {
                physicalHandsManager = GetComponentInParent<PhysicalHandsManager>();
            }
        }
    }
}