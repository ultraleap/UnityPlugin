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
    public class HardContactParent : ContactParent
    {
#if UNITY_6000_0_OR_NEWER
        public PhysicsMaterial physicsMaterial;
#else
        public PhysicMaterial physicsMaterial;
#endif 

        #region Settings
        public float maxPalmVelocity = 300f;
        public float minFingerVelocity = 50f, maxFingerVelocity = 200f;
        public float teleportDistance = 0.1f;
        public float boneMass = 0.1f, boneStiffness = 100f, grabbedBoneStiffness = 10f, boneDamping = 1f;

        public bool useProjectPhysicsIterations = false;
        public int handSolverIterations = 30, handSolverVelocityIterations = 20;

        public float contactEnterDistance = 0.002f, contactExitDistance = 0.012f;
        public float contactThumbEnterDistance = 0.005f, contactThumbExitDistance = 0.02f;
        #endregion

        internal override void GenerateHands()
        {
            if (physicsMaterial == null)
            {
                physicsMaterial = CreateHandPhysicsMaterial();
            }

            GenerateHandsObjects(typeof(HardContactHand));
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