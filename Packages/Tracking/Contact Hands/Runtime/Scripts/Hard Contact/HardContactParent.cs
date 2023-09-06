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
        [SerializeField, Tooltip("The velocity that the hand will reduce down to, the further it gets away from the original data hand. " +
                "Increasing this number will cause the hand to appear \"stronger\" when pushing into objects, if less stable.")]
        internal float minPalmVelocity = 50f;
        [SerializeField, Tooltip("The velocity at which the hand will move when not contacting or grabbing any object. Reducing this number may result in additional hand latency.")]
        internal float maxPalmVelocity = 300f;
        [SerializeField]
        internal float minPalmAngularVelocity = 6000f, maxPalmAngularVelocity = 8000f;
        [SerializeField]
        internal float minFingerVelocity = 50f, maxFingerVelocity = 200f;
        [Range(0.01f, 0.5f), Tooltip("The maximum distance at which the hand will then jump back to the data hand.")]
        internal float teleportDistance = 0.1f;
        [SerializeField]
        internal float maxWeight = 15f;
        [SerializeField]
        internal float boneMass = 0.1f, boneStiffness = 100f, boneForceLimit = 1000f;

        [SerializeField]
        internal bool useProjectPhysicsIterations = false;
        [SerializeField]
        internal int handSolverIterations = 30, handSolverVelocityIterations = 20;

        internal float contactEnterDistance = 0.004f, contactExitDistance = 0.012f;
        internal float contactThumbEnterDistance = 0.005f, contactThumbExitDistance = 0.02f;
        #endregion

        internal override void GenerateHands()
        {
            _physicsMaterial = CreateHandPhysicsMaterial();
            GenerateHandsObjects(typeof(HardContactHand));
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