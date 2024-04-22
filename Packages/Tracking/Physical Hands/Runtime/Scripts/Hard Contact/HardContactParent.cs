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
    public class HardContactParent : ContactParent
    {
        [SerializeField, HideInInspector]
        private PhysicMaterial _physicsMaterial;
        internal PhysicMaterial PhysicsMaterial => _physicsMaterial;

        #region Settings
        [SerializeField, HideInInspector, Tooltip("The velocity at which the hand will move when not contacting or grabbing any object. Reducing this number may result in additional hand latency.")]
        internal float maxPalmVelocity = 300f;
        [SerializeField, HideInInspector]
        internal float minFingerVelocity = 50f, maxFingerVelocity = 200f;
        [SerializeField, HideInInspector, Range(0.01f, 0.5f), Tooltip("The maximum distance at which the hand will then jump back to the data hand.")]
        internal float teleportDistance = 0.1f;
        [SerializeField, HideInInspector]
        internal float boneMass = 0.1f, boneStiffness = 100f, grabbedBoneStiffness = 10f, boneDamping = 1f;

        [SerializeField, HideInInspector]
        internal bool useProjectPhysicsIterations = false;
        [SerializeField, HideInInspector]
        internal int handSolverIterations = 30, handSolverVelocityIterations = 20;

        internal float contactEnterDistance = 0.002f, contactExitDistance = 0.012f;
        internal float contactThumbEnterDistance = 0.005f, contactThumbExitDistance = 0.02f;

        #endregion

        internal override void GenerateHands()
        {
            _physicsMaterial = CreateHandPhysicsMaterial();
            GenerateHandsObjects(typeof(HardContactHand));
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