using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class HardContactHand : ContactHand
    {
        private const int RESET_FRAME_COUNT = 2, TELEPORT_FRAME_COUNT = 10;

        private HardContactParent hardContactParent => contactParent as HardContactParent;

        private bool _hasReset = false;
        private int _resetCounter = 2, _teleportFrameCount = 10;
        private int _layerMask = 0;


        protected override void ProcessOutputHand()
        {
        }

        internal override void BeginHand(Hand hand)
        {
            if(!resetting)
            {
                _resetCounter = RESET_FRAME_COUNT;
                _hasReset = false;
                resetting = true;
                _teleportFrameCount = TELEPORT_FRAME_COUNT;
                ghosted = true;
                for (int i = 0; i < 32; i++)
                {
                    if (!Physics.GetIgnoreLayerCollision(contactManager.HandsLayer, i))
                    {
                        _layerMask = _layerMask | 1 << i;
                    }
                }
                gameObject.SetActive(true);
            }
            else
            {

            }
        }

        internal override void FinishHand()
        {
            tracked = false;
            resetting = false;
            gameObject.SetActive(false);
        }

        private void ResetHardContactHand(bool active)
        {
            ChangeHandLayer(contactManager.HandsResetLayer);

        }

        internal override void GenerateHandLogic()
        {
            GenerateHandObjects(typeof(HardContactBone));

            ((HardContactBone)palmBone).SetupBoneBody();
            // Set the colliders to ignore eachother
            foreach (var bone in bones)
            {
                ((HardContactBone)bone).SetupBoneBody();
                Physics.IgnoreCollision(palmBone.palmCollider, bone.boneCollider);

                foreach (var bone2 in bones)
                {
                    if (bone != bone2)
                    {
                        Physics.IgnoreCollision(bone.boneCollider, bone2.boneCollider);
                    }
                }
            }
        }

        internal override void PostFixedUpdateHand()
        {
        }

        protected override void UpdateHandLogic(Hand hand)
        {
            
        }
    }
}