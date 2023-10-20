using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Leap.Unity.PhysicalHands
{
    public class SoftContactParent : ContactParent
    {
        [SerializeField, HideInInspector]
        private PhysicMaterial _physicsMaterial;
        public PhysicMaterial PhysicsMaterial => _physicsMaterial;

        internal override void GenerateHands()
        {
            _physicsMaterial = CreateHandPhysicsMaterial();
            GenerateHandsObjects(typeof(SoftContactHand));
        }

        internal override void PostFixedUpdateFrameLogic()
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
