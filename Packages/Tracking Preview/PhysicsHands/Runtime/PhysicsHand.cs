/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Leap.Unity.Interaction.PhysicsHands
{
    public class PhysicsHand : MonoBehaviour
    {
        [System.Serializable]
        public class Hand
        {
            public const int FINGERS = 5, BONES = 3;
            public const float TRIGGER_DISTANCE = 0.004f;

            // You can change this to move your helper physics checks further forward
            public float triggerDistance = TRIGGER_DISTANCE;

            [HideInInspector]
            public Vector3 oldPosition;
            public GameObject gameObject, rootObject;
            public Transform transform;

            public PhysicsBone palmBone;
            public ArticulationBody palmBody;
            public BoxCollider palmCollider;

            public PhysicsBone[] jointBones;
            public ArticulationBody[] jointBodies;
            public CapsuleCollider[] jointColliders;

            [HideInInspector]
            public Quaternion[] defaultRotations;

            public float strength;
            [HideInInspector]
            public float stiffness, forceLimit;
            public float boneMass;

            [HideInInspector]
            public PhysicMaterial physicMaterial;
        }

        private PhysicsProvider _physicsProvider;
        public PhysicsProvider Provider => _physicsProvider;
        [SerializeField]
        private Hand _physicsHand;

        public ArticulationBody[] Bodies
        {
            get
            {
                if (_physicsHand == null)
                    return null;
                return _physicsHand.jointBodies;
            }
        }

        private int _lastFrameTeleport = 0;
        private bool _ghosted = false, _hasReset = false;
        private int _layerMask = 0;

        private float _graspMass = 0;

        [SerializeField]
        private Chirality _handedness;

        private Leap.Hand _originalLeapHand, _leapHand;

        public Chirality Handedness
        {
            get { return _handedness; }
            set { _handedness = value; }
        }

        public Leap.Hand GetOriginalLeapHand()
        {
            return _originalLeapHand;
        }

        public Leap.Hand GetLeapHand()
        {
            if (_hasReset)
            {
                return _leapHand;
            }
            else
            {
                return null;
            }
        }

        public Hand GetPhysicsHand()
        {
            return _physicsHand;
        }

        public void SetPhysicsHand(Hand hand)
        {
            _physicsHand = hand;
        }

        public Action OnBeginPhysics, OnUpdatePhysics, OnFinishPhysics;

        private int _resetWait = 0;
        private int _teleportFrameCount = 0;
        private Collider[] _colliderCache = new Collider[10];

        private Vector3 _originalOldPosition = Vector3.zero;
        private float _graspingDelta = 0;
        private float _graspingDeltaCurrent = 0;

        private int[] _graspingFingers = new int[5];
        private bool[] _wasGraspingBones;
        private float[] _graspingXDrives;

        private bool _hasGenerated = false;

        private bool _wasGrasping = false;
        private bool _isGrasping = false;
        public bool IsGrasping => _isGrasping;

        private Dictionary<Rigidbody, Collider[]> _ignoredObjects = new Dictionary<Rigidbody, Collider[]>();
        private Dictionary<Rigidbody, float> _ignoredTimeouts = new Dictionary<Rigidbody, float>();

        public bool IsTracked { get; private set; }

        // This is the distance between the raw data hand that the physics hand is derived from.
        public float DistanceFromDataHand
        {
            get
            {
                if (_originalLeapHand == null || _physicsHand == null || _physicsHand.transform == null) return -1;
                return Vector3.Distance(_originalLeapHand.PalmPosition, _physicsHand.transform.position);
            }
        }

        public Vector3 GetTipPosition(int index)
        {
            if (GetLeapHand() == null)
            {
                return Vector3.zero;
            }
            Vector3 outPos;
            PhysExts.ToWorldSpaceCapsule(_physicsHand.jointColliders[(Hand.FINGERS * index) + Hand.BONES - 1], out outPos, out var temp, out var temp2);
            return outPos;
        }

        private void Start()
        {
            _physicsProvider = GetComponentInParent<PhysicsProvider>();

            _wasGraspingBones = new bool[Hand.BONES * Hand.FINGERS];
            _graspingXDrives = new float[Hand.BONES * Hand.FINGERS];
            _originalLeapHand = new Leap.Hand();
            _leapHand = new Leap.Hand();
            _hasReset = false;

#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                return;
            }
#endif

            gameObject.SetActive(false);
            _physicsHand.gameObject.SetActive(false);
        }

        public void BeginHand(Leap.Hand hand)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                return;
            }
#endif

            _originalLeapHand = hand.CopyFrom(hand);

            int myLayer = _physicsProvider.HandsLayer;
            for (int i = 0; i < 32; i++)
            {
                if (!Physics.GetIgnoreLayerCollision(myLayer, i))
                {
                    _layerMask = _layerMask | 1 << i;
                }
            }

            _ghosted = true;
            // Longer wait till contact
            _teleportFrameCount = 10;
            _resetWait = 2;
            _hasReset = false;

            gameObject.SetActive(true);
            _physicsHand.gameObject.SetActive(false);
        }

        #region Hand Reset

        protected void ResetPhysicsHand(bool active)
        {
            if (_physicsHand == null || _physicsHand.transform == null)
                return;

            ChangeHandLayer(_physicsProvider.HandsResetLayer);

            if (active)
            {
                _physicsHand.palmBody.immovable = false;
                _originalOldPosition = _originalLeapHand.PalmPosition;

                _physicsHand.transform.position = _originalLeapHand.PalmPosition;
                _physicsHand.transform.rotation = _originalLeapHand.Rotation;
                _physicsHand.palmBody.WakeUp();
                _physicsHand.palmBody.TeleportRoot(_physicsHand.transform.position, _physicsHand.transform.rotation);

                PhysicsHandsUtils.ResetPhysicsHandSizes(_physicsHand, _originalLeapHand);

                _lastFrameTeleport = Time.frameCount;

                for (int fingerIndex = 0; fingerIndex < Hand.FINGERS; fingerIndex++)
                {
                    for (int jointIndex = 0; jointIndex < Hand.BONES; jointIndex++)
                    {
                        Bone prevBone = _originalLeapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
                        Bone bone = _originalLeapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1));
                        int boneArrayIndex = fingerIndex * Hand.BONES + jointIndex;
                        _wasGraspingBones[boneArrayIndex] = false;
                        ArticulationBody body = _physicsHand.jointBodies[boneArrayIndex];

                        float xTargetAngle = PhysicsHandsUtils.CalculateXTargetAngle(prevBone, bone, fingerIndex, jointIndex);
                        body.xDrive = new ArticulationDrive()
                        {
                            stiffness = _physicsHand.stiffness * _physicsHand.strength,
                            forceLimit = _wasGraspingBones[boneArrayIndex] ? 0.1f / Time.fixedDeltaTime : _physicsHand.forceLimit * _physicsHand.strength / Time.fixedDeltaTime,
                            damping = body.xDrive.damping,
                            lowerLimit = body.xDrive.lowerLimit,
                            upperLimit = _graspingFingers[fingerIndex] > jointIndex ? body.xDrive.target : _physicsHand.jointBones[boneArrayIndex].OriginalXDriveLimit,
                            target = _wasGraspingBones[boneArrayIndex] ? Mathf.Clamp(xTargetAngle, body.xDrive.lowerLimit, _graspingXDrives[boneArrayIndex]) : xTargetAngle
                        };

                        if (jointIndex == 0)
                        {
                            float yTargetAngle = PhysicsHandsUtils.CalculateYTargetAngle(prevBone, bone);

                            body.yDrive = new ArticulationDrive()
                            {
                                stiffness = _physicsHand.stiffness * _physicsHand.strength,
                                forceLimit = _physicsHand.forceLimit * _physicsHand.strength / Time.fixedDeltaTime,
                                damping = body.yDrive.damping,
                                upperLimit = body.yDrive.upperLimit,
                                lowerLimit = body.yDrive.lowerLimit,
                                target = yTargetAngle
                            };
                        }
                        body.WakeUp();
                    }
                }
                _ghosted = true;

            }
            else
            {
                _ghosted = true;
            }

            _physicsHand.gameObject.SetActive(active);
        }

        public void SetGrasping(bool isGrasping)
        {
            _isGrasping = isGrasping;
        }

        public void SetGraspingMass(float mass)
        {
            _graspMass = mass;
        }

        #endregion

        public void UpdateHand(Leap.Hand hand)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                return;
            }
#endif

            _originalLeapHand.CopyFrom(hand);

            if (!_hasReset && _resetWait > 0)
            {
                _resetWait--;
                if (_resetWait == 0)
                {
                    DelayedReset();
                    _hasReset = true;
                }
                else
                {
                    // We don't want hands until they're stable.
                    return;
                }
            }

            UpdateSettings();

            if (!IsGrasping && _graspingDeltaCurrent > 0)
            {
                _graspingDeltaCurrent -= Vector3.Distance(_originalOldPosition, _originalLeapHand.PalmPosition);
            }

            PhysicsHandsUtils.UpdatePhysicsPalm(ref _physicsHand,
                // If the hand was grasping then we want to smoothly interpolate back to where it was based on distance
                !IsGrasping && _graspingDeltaCurrent > 0 ? Vector3.Lerp(_physicsHand.transform.position, _originalLeapHand.PalmPosition, Mathf.InverseLerp(_graspingDelta, 0, _graspingDeltaCurrent)) : _originalLeapHand.PalmPosition,
                !IsGrasping && _graspingDeltaCurrent > 0 ? Quaternion.Slerp(_physicsHand.transform.rotation, _originalLeapHand.Rotation, Mathf.InverseLerp(_graspingDelta, 0, _graspingDeltaCurrent)) : _originalLeapHand.Rotation,
                // Interpolate the object if it's heavier
                _isGrasping && _physicsProvider.InterpolatingMass && _graspMass > 1 ? Mathf.InverseLerp(0.001f, _physicsProvider.MaxMass, _graspMass).EaseOut() : 0f,
                // Reduce force of hand as it gets further from the original data hand
                !_ghosted && !_isGrasping && _graspingDeltaCurrent > 0 ? Mathf.InverseLerp(_physicsProvider.HandTeleportDistance * 0.5f, _physicsProvider.HandTeleportDistance, DistanceFromDataHand).EaseOut() : 0f);

            // Fix the hand if it gets into a bad situation by teleporting and holding in place until its bad velocities disappear
            HandleTeleportingHands();

            // Update the palm collider with the distance between knuckles to wrist + palm width
            _physicsHand.palmCollider.size = Vector3.Lerp(_physicsHand.palmCollider.size, PhysicsHandsUtils.CalculatePalmSize(_originalLeapHand), Time.fixedDeltaTime);

            // Iterate through the bones in the hand, applying drive forces
            for (int fingerIndex = 0; fingerIndex < Hand.FINGERS; fingerIndex++)
            {
                Bone knuckleBone = _originalLeapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(0));
                _graspingFingers[fingerIndex] = -1;
                if (IsGrasping)
                {
                    for (int jointIndex = 0; jointIndex < Hand.BONES; jointIndex++)
                    {
                        int boneArrayIndex = fingerIndex * Hand.BONES + jointIndex;
                        if (_physicsHand.jointBones[boneArrayIndex].IsGrasping)
                        {
                            _graspingFingers[fingerIndex] = jointIndex;
                            _wasGraspingBones[boneArrayIndex] = true;
                        }
                    }
                }

                for (int jointIndex = 0; jointIndex < Hand.BONES; jointIndex++)
                {
                    Bone prevBone = _originalLeapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
                    Bone bone = _originalLeapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1));

                    int boneArrayIndex = fingerIndex * Hand.BONES + jointIndex;
                    ArticulationBody body = _physicsHand.jointBodies[boneArrayIndex];

                    if (_wasGraspingBones[boneArrayIndex])
                    {
                        _graspingXDrives[boneArrayIndex] = _physicsHand.jointBones[boneArrayIndex].XDriveLimit;
                    }

                    // Trying to get on the fly scaling working but just makes the bones dogspin
                    if (jointIndex > 0)
                    {
                        PhysicsHandsUtils.InterpolateBoneSize(_physicsHand.jointBodies[boneArrayIndex], _physicsHand.jointBones[boneArrayIndex], _physicsHand.jointColliders[boneArrayIndex],
                            prevBone.PrevJoint, prevBone.Rotation, bone.PrevJoint,
                            bone.Width, bone.Length, Time.fixedDeltaTime);
                    }
                    else
                    {
                        PhysicsHandsUtils.InterpolateBoneSize(_physicsHand.jointBodies[boneArrayIndex], _physicsHand.jointBones[boneArrayIndex], _physicsHand.jointColliders[boneArrayIndex],
                            _originalLeapHand.PalmPosition, _originalLeapHand.Rotation, fingerIndex == 0 ? knuckleBone.PrevJoint : knuckleBone.NextJoint,
                            bone.Width, bone.Length, Time.fixedDeltaTime);
                    }

                    float xTargetAngle = PhysicsHandsUtils.CalculateXTargetAngle(prevBone, bone, fingerIndex, jointIndex);

                    Mathf.InverseLerp(body.xDrive.lowerLimit, _physicsHand.jointBones[boneArrayIndex].OriginalXDriveLimit, xTargetAngle);

                    // Clamp the max until we've moved the bone to a lower amount than originally grasped at
                    if (_wasGraspingBones[boneArrayIndex] && (xTargetAngle < _graspingXDrives[boneArrayIndex] || Mathf.InverseLerp(body.xDrive.lowerLimit, _physicsHand.jointBones[boneArrayIndex].OriginalXDriveLimit, xTargetAngle) < .25f))
                    {
                        _wasGraspingBones[boneArrayIndex] = false;
                    }

                    body.xDrive = new ArticulationDrive()
                    {
                        stiffness = _physicsHand.stiffness * _physicsHand.strength,
                        forceLimit = _wasGraspingBones[boneArrayIndex] ? 0.1f / Time.fixedDeltaTime : _physicsHand.forceLimit * _physicsHand.strength / Time.fixedDeltaTime,
                        damping = body.xDrive.damping,
                        lowerLimit = body.xDrive.lowerLimit,
                        upperLimit = _graspingFingers[fingerIndex] > jointIndex ? body.xDrive.target : _physicsHand.jointBones[boneArrayIndex].OriginalXDriveLimit,
                        target = _wasGraspingBones[boneArrayIndex] ? Mathf.Clamp(xTargetAngle, body.xDrive.lowerLimit, _graspingXDrives[boneArrayIndex]) : xTargetAngle
                    };

                    if (jointIndex == 0)
                    {
                        float yTargetAngle = PhysicsHandsUtils.CalculateYTargetAngle(prevBone, bone);

                        body.yDrive = new ArticulationDrive()
                        {
                            stiffness = _physicsHand.stiffness * _physicsHand.strength,
                            forceLimit = _physicsHand.forceLimit * _physicsHand.strength / Time.fixedDeltaTime,
                            damping = body.yDrive.damping,
                            upperLimit = body.yDrive.upperLimit,
                            lowerLimit = body.yDrive.lowerLimit,
                            target = yTargetAngle
                        };
                    }
                }
            }

            HandleIgnoredObjects();

            _wasGrasping = IsGrasping;
            _originalOldPosition = _originalLeapHand.PalmPosition;

            if (IsGrasping)
            {
                // Makes the hands a bit smoother when we release
                _graspingDelta = DistanceFromDataHand * 1.5f;
                _graspingDeltaCurrent = _graspingDelta;
            }

            PhysicsHandsUtils.ConvertPhysicsToLeapHand(_physicsHand, ref _leapHand, _originalLeapHand, Time.fixedDeltaTime);

            OnUpdatePhysics?.Invoke();
        }

        private void UpdateSettings()
        {
            if (_physicsProvider == null)
            {
                return;
            }
            _physicsHand.strength = _physicsProvider.Strength;
            _physicsHand.boneMass = _physicsProvider.PerBoneMass;
        }

        private void DelayedReset()
        {
            if (!_hasGenerated)
            {
                PhysicsHandsUtils.SetupHand(_physicsHand, _originalLeapHand);
                _physicsHand.gameObject.SetActive(true);
                _hasGenerated = true;
            }
            else
            {
                ResetPhysicsHand(true);
            }
            _leapHand.CopyFrom(_originalLeapHand);
            _hasReset = true;
            OnBeginPhysics?.Invoke();
        }

        public void FinishHand()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                return;
            }
#endif

            _hasReset = false;

            ResetPhysicsHand(false);

            OnFinishPhysics?.Invoke();
            gameObject.SetActive(false);
            _physicsHand.gameObject.SetActive(false);
        }

        private void HandleTeleportingHands()
        {
            // Fix the hand if it gets into a bad situation by teleporting and holding in place until its bad velocities disappear
            if (DistanceFromDataHand > (IsGrasping ? _physicsProvider.HandGraspTeleportDistance : _physicsProvider.HandTeleportDistance))
            {
                ResetPhysicsHand(true);
                //_palmBody.TeleportRoot(_hand.PalmPosition.ToVector3(), _hand.Rotation.ToQuaternion());
                // Don't need to wait for the hand to reset as much here
                _teleportFrameCount = 5;

                _ghosted = true;
            }

            if (Time.frameCount - _lastFrameTeleport >= _teleportFrameCount && _ghosted && !IsAnyObjectInHandRadius())
            {
                ChangeHandLayer(_physicsProvider.HandsLayer);

                _ghosted = false;
            }
        }

        private void HandleIgnoredObjects()
        {
            if (_ignoredObjects.Count > 0)
            {
                bool rebuild = false;
                foreach (var timePair in _ignoredTimeouts)
                {
                    _ignoredTimeouts[timePair.Key] -= Time.fixedDeltaTime;
                    if (_ignoredTimeouts[timePair.Key] <= 0)
                    {
                        rebuild = true;
                    }
                }
                if (rebuild)
                {
                    _ignoredTimeouts = _ignoredTimeouts.Where(pair => pair.Value > 0).ToDictionary(pair => pair.Key, pair => pair.Value);
                }

                var nonContacting = _ignoredObjects.Where(pair => !IsObjectInHandRadius(pair.Key)).Select(pair => pair.Key);

                foreach (var rigid in nonContacting)
                {
                    if (!_ignoredTimeouts.ContainsKey(rigid))
                    {
                        TogglePhysicsIgnore(rigid, false);
                    }
                }
            }
        }

        private void ChangeHandLayer(SingleLayer layer)
        {
            gameObject.layer = layer;
            _physicsHand.gameObject.layer = layer;
            for (int i = 0; i < _physicsHand.jointBodies.Length; i++)
            {
                _physicsHand.jointBodies[i].gameObject.layer = layer;
            }
        }

        /// <summary>
        /// Used to make sure that the hand is not going to make contact with any object within the scene. Adjusting radius will inflate the joints. This prevents hands from attacking objects when they swap back to contacting.
        /// </summary>
        private bool IsAnyObjectInHandRadius(float radius = 0.005f)
        {
            int overlappingColliders = PhysExts.OverlapBoxNonAllocOffset(_physicsHand.palmCollider, Vector3.zero, _colliderCache, _layerMask, QueryTriggerInteraction.Ignore, radius);
            for (int i = 0; i < overlappingColliders; i++)
            {
                if (WillColliderAffectHand(_colliderCache[i]))
                {
                    return true;
                }
            }

            foreach (var collider in _physicsHand.jointColliders)
            {
                overlappingColliders = PhysExts.OverlapCapsuleNonAllocOffset(collider, Vector3.zero, _colliderCache, _layerMask, QueryTriggerInteraction.Ignore, radius);
                for (int i = 0; i < overlappingColliders; i++)
                {
                    if (WillColliderAffectHand(_colliderCache[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool WillColliderAffectHand(Collider collider)
        {
            if (collider.attachedRigidbody != null && collider.attachedRigidbody.TryGetComponent<PhysicsIgnoreHelpers>(out var temp))
            {
                return false;
            }
            if (_physicsHand.gameObject != collider.gameObject && !_physicsHand.jointBodies.Select(x => x.gameObject).Contains(collider.gameObject))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks to see whether the hand will be in contact with a specified object. Radius can be used to inflate the joints.
        /// </summary>
        private bool IsObjectInHandRadius(Rigidbody rigid, float radius = 0f)
        {
            if (rigid == null)
                return false;

            int overlappingColliders = PhysExts.OverlapBoxNonAllocOffset(_physicsHand.palmCollider, Vector3.zero, _colliderCache, _layerMask, QueryTriggerInteraction.Ignore, radius);
            for (int i = 0; i < overlappingColliders; i++)
            {
                if (_colliderCache[i].attachedRigidbody == rigid)
                {
                    return true;
                }
            }
            foreach (var collider in _physicsHand.jointColliders)
            {
                overlappingColliders = PhysExts.OverlapCapsuleNonAllocOffset(collider, Vector3.zero, _colliderCache, _layerMask, QueryTriggerInteraction.Ignore, radius);
                for (int i = 0; i < overlappingColliders; i++)
                {
                    if (_colliderCache[i].attachedRigidbody == rigid)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Disables all collisions between a rigidbody and hand. Will automatically handle all colliders on the rigidbody. Timeout lets you specify a minimum time to ignore collisions for.
        /// </summary>
        public void IgnoreCollision(Rigidbody rigid, float timeout = 0)
        {
            TogglePhysicsIgnore(rigid, true, timeout);
        }

        private void TogglePhysicsIgnore(Rigidbody rigid, bool ignore, float timeout = 0)
        {
            Collider[] colliders = rigid.GetComponentsInChildren<Collider>(true);
            foreach (var collider in colliders)
            {
                Physics.IgnoreCollision(collider, _physicsHand.palmCollider, ignore);
                foreach (var boneCollider in _physicsHand.jointColliders)
                {
                    Physics.IgnoreCollision(collider, boneCollider, ignore);
                }
            }
            if (ignore)
            {
                if (!_ignoredObjects.ContainsKey(rigid))
                {
                    _ignoredObjects.Add(rigid, colliders);
                }
                if(timeout > 0)
                {
                    if (_ignoredTimeouts.ContainsKey(rigid))
                    {
                        _ignoredTimeouts[rigid] = timeout;
                    }
                    else
                    {
                        _ignoredTimeouts.Add(rigid, timeout);
                    }
                }
            }
            else
            {
                if (_ignoredObjects.ContainsKey(rigid))
                {
                    _ignoredObjects.Remove(rigid);
                }
            }
        }
    }
}