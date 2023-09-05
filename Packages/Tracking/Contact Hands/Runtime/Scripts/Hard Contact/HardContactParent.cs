using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class HardContactParent : ContactParent
    {

        [SerializeField, HideInInspector]
        private PhysicMaterial _physicsMaterial;
        public PhysicMaterial PhysicsMaterial => _physicsMaterial;

        private HardContactHand leftHardContactHand => leftHand as HardContactHand;
        private HardContactHand rightHardContactHand => rightHand as HardContactHand;

        #region Settings
        [SerializeField]
        internal float minimumPalmVelocity, maximumPalmVelocity;
        [SerializeField]
        internal float minimumPalmAngularVelocity, maximumPalmAngularVelocity;
        [SerializeField]
        internal float minimumFingerVelocity, maximumFingerVelocity;
        [SerializeField]
        internal float maximumDistance, maximumWeight;
        [SerializeField]
        internal float boneStiffness;


        public const float CONTACT_ENTER_DISTANCE = 0.004f, CONTACT_EXIT_DISTANCE = 0.012f;
        public const float CONTACT_THUMB_ENTER_DISTANCE = 0.005f, CONTACT_THUMB_EXIT_DISTANCE = 0.02f;
        #endregion

        internal override void GenerateHands()
        {
            _physicsMaterial = CreateHandPhysicsMaterial();
            GenerateHandsObjects(typeof(HardContactHand));
        }

        internal override void PostFixedUpdateFrame()
        {

        }

        private static PhysicMaterial CreateHandPhysicsMaterial()
        {
            PhysicMaterial material = new PhysicMaterial("HandPhysics");

            material.dynamicFriction = 1f;
            material.staticFriction = 1f;
            material.frictionCombine = PhysicMaterialCombine.Average;
            material.bounceCombine = PhysicMaterialCombine.Minimum;

            return material;
        }
    }
}