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
    public class SoftContactParent : ContactParent
    {
        [SerializeField, HideInInspector]
        private PhysicsMaterial _physicsMaterial;
        internal PhysicsMaterial PhysicsMaterial => _physicsMaterial;

        internal override void GenerateHands()
        {
            _physicsMaterial = CreateHandPhysicsMaterial();
            GenerateHandsObjects(typeof(SoftContactHand));
        }

        private static PhysicsMaterial CreateHandPhysicsMaterial()
        {
            PhysicsMaterial material = new PhysicsMaterial("HandPhysics");

            material.dynamicFriction = 1f;
            material.staticFriction = 1f;
            material.frictionCombine = PhysicsMaterialCombine.Average;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;

            return material;
        }

    }
}