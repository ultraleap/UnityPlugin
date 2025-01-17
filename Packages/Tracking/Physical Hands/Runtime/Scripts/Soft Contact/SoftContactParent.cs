/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.PhysicalHands
{
    public class SoftContactParent : ContactParent
    {
        [SerializeField, HideInInspector]
#if UNITY_6000_0_OR_NEWER
        private PhysicsMaterial _physicsMaterial;
        internal PhysicsMaterial PhysicsMaterial => _physicsMaterial;
#else
        private PhysicMaterial _physicsMaterial;
        internal PhysicMaterial PhysicsMaterial => _physicsMaterial;
#endif

        internal override void GenerateHands()
        {
            _physicsMaterial = CreateHandPhysicsMaterial();
            GenerateHandsObjects(typeof(SoftContactHand));
        }

#if UNITY_6000_0_OR_NEWER
        private static PhysicsMaterial CreateHandPhysicsMaterial()
        {
            PhysicsMaterial material = new PhysicsMaterial("HandPhysics");
            material.frictionCombine = PhysicsMaterialCombine.Average;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;
#else
        private static PhysicMaterial CreateHandPhysicsMaterial()
        {
            PhysicMaterial material = new PhysicMaterial("HandPhysics");
            material.frictionCombine = PhysicMaterialCombine.Average;
            material.bounceCombine = PhysicMaterialCombine.Minimum;
#endif 
            material.dynamicFriction = 1f;
            material.staticFriction = 1f;
            return material;
        }

    }
}