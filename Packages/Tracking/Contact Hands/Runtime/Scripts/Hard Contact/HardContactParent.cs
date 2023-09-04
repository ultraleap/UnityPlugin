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