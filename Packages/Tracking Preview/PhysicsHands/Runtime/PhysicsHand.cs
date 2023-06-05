/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
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
using System.Collections;

namespace Leap.Unity.Interaction.PhysicsHands
{
    public class PhysicsHand : MonoBehaviour
    {
        [System.Serializable]
        public class Hand
        {
            public const int FINGERS = 5, BONES = 3;
            public const float TRIGGER_DISTANCE = 0.004f;
            public const float CONTACT_ENTER_DISTANCE = 0.0015f, CONTACT_EXIT_DISTANCE = 0.01f;
            public const float CONTACT_THUMB_ENTER_DISTANCE = 0.0035f, CONTACT_THUMB_EXIT_DISTANCE = 0.018f;
            // Used as velocity * fixedDeltaTime
            public const float MAXIMUM_PALM_VELOCITY = 300f, MINIMUM_PALM_VELOCITY = 50f, MAXIMUM_FINGER_VELOCITY = 200f, MINIMUM_FINGER_VELOCITY = 50f;
            // Used as angularVelocity * fixedDeltaTime
            public const float MAXIMUM_PALM_ANGULAR_VELOCITY = 8000f, MINIMUM_PALM_ANGULAR_VELOCITY = 6000f;

            [Tooltip("The distance that bones will have their radius inflated by when calculating if an object is grabbed.")]
            public float triggerDistance = TRIGGER_DISTANCE;
            // You can change this to reduce the overall speed of the hands
            [Tooltip("The velocity at which the hand will move when not contacting or grabbing any object. Reducing this number may result in additional hand latency.")]
            public float maximumPalmVelocity = MAXIMUM_PALM_VELOCITY;
            [Tooltip("The velocity that the hand will reduce down to, the further it gets away from the original data hand. " +
                "Increasing this number will cause the hand to appear \"stronger\" when pushing into objects, if less stable.")]
            public float minimumPalmVelocity = MINIMUM_PALM_VELOCITY;

            public float maximumFingerVelocity = MAXIMUM_FINGER_VELOCITY;

            public float minimumFingerVelocity = MINIMUM_FINGER_VELOCITY;

            public float maximumPalmAngularVelocity = MAXIMUM_PALM_ANGULAR_VELOCITY;

            public float minimumPalmAngularVelocity = MINIMUM_PALM_ANGULAR_VELOCITY;

            [HideInInspector]
            public float currentPalmVelocity = MAXIMUM_PALM_VELOCITY, currentPalmAngularVelocity = MAXIMUM_PALM_ANGULAR_VELOCITY;
            [HideInInspector]
            public float currentPalmVelocityInterp = 0f;
            [HideInInspector]
            public float currentPalmWeightInterp = 0f, currentPalmWeight = 0f;

            [HideInInspector]
            public Vector3 oldPosition;
            public GameObject gameObject, rootObject;
            [HideInInspector]
            public Vector3 previousDataPosition, computedPhysicsPosition;
            [HideInInspector]
            public Vector3 elbowPosition;
            [HideInInspector]
            public float computedHandDistance;
            [HideInInspector]
            public Quaternion previousDataRotation, computedPhysicsRotation;
            public Transform transform;

            public PhysicsBone palmBone;
            public ArticulationBody palmBody;
            public BoxCollider palmCollider;

            public PhysicsBone[] jointBones;
            public ArticulationBody[] jointBodies;
            public CapsuleCollider[] jointColliders;
            [HideInInspector]
            public int[] overRotationFrameCount;

            [HideInInspector]
            public Quaternion[] defaultRotations;

            [HideInInspector]
            public bool justGhosted = false;

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

        private int[] _graspingFingers = new int[5];
        private bool[] _wasGraspingBones = new bool[Hand.BONES * Hand.FINGERS];
        private bool[] _justGraspedBone = new bool[Hand.BONES * Hand.FINGERS];
        private float[] _graspingXDrives = new float[Hand.BONES * Hand.FINGERS];
        private float[] _graspingFingerDistance = new float[5];
        private float[] _xForceLimits = new float[Hand.BONES * Hand.FINGERS];
        private float[] _xDampening = new float[Hand.BONES * Hand.FINGERS];
        private bool[] _fingerContacting = new bool[5];

        private WaitForFixedUpdate _waitForFixedUpdate;

        private bool _hasGenerated = false;
        private float _timeOnReset = 0;
        private float _currentResetLerp { get { return _timeOnReset == 0 ? 1 : Mathf.InverseLerp(0.1f, 0.25f, Time.time - _timeOnReset); } }

        private bool _isCloseToObject = false;
        public bool IsCloseToObject => _isCloseToObject;

        private bool _isContacting = false;
        public bool IsContacting => _isContacting;

        private bool _wasGrasping = false;
        private bool _isGrasping = false;
        public bool IsGrasping => _isGrasping;


        private List<IgnoreData> _ignoredData = new List<IgnoreData>();
        private class IgnoreData
        {
            public Rigidbody rigid;
            public Collider[] colliders;
            public float timeout = 0;
            public float radius = 0;

            public IgnoreData(Rigidbody rigid, Collider[] colliders)
            {
                this.rigid = rigid;
                this.colliders = colliders;
            }
        }
        public bool IsTracked { get; private set; } = false;

        // This is the distance between the raw data hand that the physics hand is derived from.
        public float DistanceFromDataHand
        {
            get
            {
                if (_originalLeapHand == null || _physicsHand == null || _physicsHand.transform == null) return -1;
                return Vector3.Distance(_originalLeapHand.PalmPosition, _physicsHand.transform.position);
            }
        }

        private void Start()
        {
            _physicsProvider = GetComponentInParent<PhysicsProvider>();

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

                CachePositions();

                _physicsHand.transform.position = _originalLeapHand.PalmPosition;
                _physicsHand.transform.rotation = _originalLeapHand.Rotation;
                _physicsHand.palmBody.TeleportRoot(_physicsHand.transform.position, _physicsHand.transform.rotation);

                _physicsHand.previousDataPosition = _originalLeapHand.PalmPosition;
                _physicsHand.previousDataRotation = _originalLeapHand.Rotation;
                _physicsHand.computedPhysicsPosition = _originalLeapHand.PalmPosition;
                _physicsHand.computedPhysicsRotation = _originalLeapHand.Rotation;
                _physicsHand.computedHandDistance = 0f;

                _physicsHand.currentPalmVelocity = _physicsHand.maximumPalmVelocity;
                _physicsHand.elbowPosition = _originalLeapHand.Arm.PrevJoint;

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
                        _xDampening[boneArrayIndex] = 1f;
                        ArticulationBody body = _physicsHand.jointBodies[boneArrayIndex];

                        float xTargetAngle = PhysicsHandsUtils.CalculateXJointAngle(prevBone.Rotation, bone.Direction);
                        body.xDrive = new ArticulationDrive()
                        {
                            stiffness = _physicsHand.stiffness,
                            forceLimit = _xForceLimits[boneArrayIndex] * Time.fixedDeltaTime,
                            damping = body.xDrive.damping,
                            lowerLimit = body.xDrive.lowerLimit,
                            upperLimit = _graspingFingers[fingerIndex] > jointIndex ? body.xDrive.target : _physicsHand.jointBones[boneArrayIndex].OriginalXDriveUpper,
                            target = _wasGraspingBones[boneArrayIndex] ? Mathf.Clamp(xTargetAngle, body.xDrive.lowerLimit, _graspingXDrives[boneArrayIndex]) : xTargetAngle
                        };

                        float yTargetAngle = PhysicsHandsUtils.CalculateYJointAngle(prevBone.Rotation, bone.Rotation);
                        body.yDrive = new ArticulationDrive()
                        {
                            stiffness = _physicsHand.stiffness,
                            forceLimit = _physicsHand.maximumPalmVelocity * Time.fixedDeltaTime,
                            damping = body.yDrive.damping,
                            upperLimit = body.yDrive.upperLimit,
                            lowerLimit = body.yDrive.lowerLimit,
                            target = yTargetAngle
                        };
                    }
                }

                _ghosted = true;
                _physicsHand.gameObject.SetActive(false);
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

            IsTracked = false;

            _originalLeapHand.CopyFrom(hand);

            if (!_hasReset && _resetWait > 0)
            {
                _resetWait--;
                if (_resetWait == 1)
                {
                    DelayedReset();
                    return;
                }
                else if (_resetWait == 0)
                {
                    CompleteReset();
                }
                else
                {
                    // We don't want hands until they're stable.
                    return;
                }
            }

            IsTracked = true;

            UpdateSettings();

            CalculateContacts();

            // Reset timer on hand release so hands quickly restore size
            if (!IsGrasping && _wasGrasping)
            {
                _timeOnReset = Time.time;
            }

            PhysicsHandsUtils.UpdatePhysicsPalm(ref _physicsHand, _originalLeapHand, _physicsProvider.HandTeleportDistance, IsContacting, IsGrasping, _physicsProvider.InterpolatingMass ? _graspMass : 1f, _physicsProvider.MaxMass);

            // Fix the hand if it gets into a bad situation by teleporting and holding in place until its bad velocities disappear
            HandleTeleportingHands(AreBonesRotatedBeyondThreshold());

            // Update the palm collider with the distance between knuckles to wrist + palm width
            _physicsHand.palmCollider.size = Vector3.Lerp(_physicsHand.palmCollider.size, PhysicsHandsUtils.CalculatePalmSize(_originalLeapHand), _currentResetLerp);

            // Iterate through the bones in the hand, applying xDrive forces
            for (int fingerIndex = 0; fingerIndex < Hand.FINGERS; fingerIndex++)
            {
                Bone knuckleBone = _originalLeapHand.Fingers[fingerIndex].Bone(0);

                for (int jointIndex = 0; jointIndex < Hand.BONES; jointIndex++)
                {
                    Bone prevBone = _originalLeapHand.Fingers[fingerIndex].Bone((Bone.BoneType)jointIndex);
                    Bone bone = _originalLeapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1));

                    int boneArrayIndex = fingerIndex * Hand.BONES + jointIndex;
                    ArticulationBody body = _physicsHand.jointBodies[boneArrayIndex];

                    // Hand physicsBone resizing, done very slowly during movement.
                    // Initial resizing is very fast (while the user is bringing their hand into the frame).
                    if (!_physicsHand.jointBones[boneArrayIndex].IsContacting)
                    {
                        if (jointIndex > 0)
                        {
                            PhysicsHandsUtils.InterpolateBoneSize(_physicsHand.jointBodies[boneArrayIndex], _physicsHand.jointBones[boneArrayIndex], _physicsHand.jointColliders[boneArrayIndex],
                                prevBone.PrevJoint, prevBone.Rotation, bone.PrevJoint,
                                bone.Width, bone.Length, Mathf.Lerp(Time.fixedDeltaTime * 10f, Time.fixedDeltaTime, _currentResetLerp));
                        }
                        else
                        {
                            PhysicsHandsUtils.InterpolateKnucklePosition(_physicsHand.jointBodies[boneArrayIndex], _physicsHand.jointBones[boneArrayIndex], _originalLeapHand, _currentResetLerp);
                            PhysicsHandsUtils.InterpolateBoneSize(_physicsHand.jointBodies[boneArrayIndex], _physicsHand.jointBones[boneArrayIndex], _physicsHand.jointColliders[boneArrayIndex],
                                _originalLeapHand.PalmPosition, _originalLeapHand.Rotation, fingerIndex == 0 ? knuckleBone.PrevJoint : knuckleBone.NextJoint,
                                bone.Width, bone.Length, Mathf.Lerp(Time.fixedDeltaTime * 10f, Time.fixedDeltaTime, _currentResetLerp));
                        }
                    }

                    float xTargetAngle = PhysicsHandsUtils.CalculateXJointAngle(prevBone.Rotation, bone.Direction);

                    if (_justGraspedBone[boneArrayIndex])
                    {
                        _graspingXDrives[boneArrayIndex] = _physicsHand.jointBodies[boneArrayIndex].jointPosition[0] * Mathf.Rad2Deg;
                        _justGraspedBone[boneArrayIndex] = false;
                    }

                    if (_wasGraspingBones[boneArrayIndex] && xTargetAngle > _graspingXDrives[boneArrayIndex])
                    {
                        _xDampening[boneArrayIndex] = 10f;
                    }
                    else
                    {
                        _xDampening[boneArrayIndex] = Mathf.Lerp(_xDampening[boneArrayIndex], 2f, Time.fixedDeltaTime * (1.0f / 0.25f));
                    }

                    if (_graspingFingerDistance[fingerIndex] != 1 && _graspingFingerDistance[fingerIndex] > (fingerIndex == 0 ? Hand.CONTACT_THUMB_ENTER_DISTANCE : Hand.CONTACT_ENTER_DISTANCE) && xTargetAngle > _graspingXDrives[boneArrayIndex])
                    {
                        _graspingXDrives[boneArrayIndex] = Mathf.Clamp(Mathf.Lerp(_graspingXDrives[boneArrayIndex], xTargetAngle, Time.fixedDeltaTime * (1.0f / 0.25f)),
                            _physicsHand.jointBones[boneArrayIndex].OriginalXDriveLower, _physicsHand.jointBones[boneArrayIndex].OriginalXDriveUpper);
                    }

                    if (_physicsHand.currentPalmVelocityInterp > 0 && _fingerContacting[fingerIndex])
                    {
                        _xForceLimits[boneArrayIndex] = Mathf.Lerp(_xForceLimits[boneArrayIndex],
                            Mathf.Lerp(_physicsHand.maximumFingerVelocity, _physicsHand.minimumFingerVelocity, _physicsHand.currentPalmVelocityInterp),
                            Time.fixedDeltaTime * (1.0f / 0.05f));
                    }
                    else
                    {
                        _xForceLimits[boneArrayIndex] = Mathf.Lerp(_xForceLimits[boneArrayIndex], _physicsHand.maximumFingerVelocity, Time.fixedDeltaTime * (1.0f / 0.5f));
                    }

                    ArticulationDrive xDrive = body.xDrive;
                    xDrive.stiffness = _physicsHand.stiffness;
                    xDrive.damping = _xDampening[boneArrayIndex];
                    xDrive.forceLimit = _xForceLimits[boneArrayIndex] * Time.fixedDeltaTime;
                    xDrive.upperLimit = _graspingFingers[fingerIndex] >= jointIndex ? _graspingXDrives[boneArrayIndex] : _physicsHand.jointBones[boneArrayIndex].OriginalXDriveUpper;
                    xDrive.target = _wasGraspingBones[boneArrayIndex] ? Mathf.Clamp(xTargetAngle, body.xDrive.lowerLimit, _graspingXDrives[boneArrayIndex]) : xTargetAngle;
                    body.xDrive = xDrive;

                    if (jointIndex == 0)
                    {
                        float yTargetAngle = PhysicsHandsUtils.CalculateYJointAngle(prevBone.Rotation, bone.Rotation);

                        ArticulationDrive yDrive = body.yDrive;
                        yDrive.damping = _xDampening[boneArrayIndex] * .75f;
                        yDrive.stiffness = _physicsHand.stiffness;
                        yDrive.forceLimit = _physicsHand.maximumPalmVelocity * Time.fixedDeltaTime;
                        yDrive.target = yTargetAngle;
                        body.yDrive = yDrive;
                    }
                }
            }

            HandleIgnoredObjects();

            _wasGrasping = IsGrasping;

            PhysicsHandsUtils.ConvertPhysicsToLeapHand(_physicsHand, ref _leapHand, _originalLeapHand, Time.fixedDeltaTime);

            _physicsHand.elbowPosition = _leapHand.Arm.PrevJoint;

            OnUpdatePhysics?.Invoke();

            CachePositions();
        }

        // Happens after the physics simulation
        internal void LateFixedUpdate()
        {
                for (int fingerIndex = 0; fingerIndex < Hand.FINGERS; fingerIndex++)
                {
                    _graspingFingerDistance[fingerIndex] = 1f;

                    _graspingFingers[fingerIndex] = -1;

                    bool hasFingerGrasped = false;

                    _fingerContacting[fingerIndex] = false;

                    for (int jointIndex = Hand.BONES - 1; jointIndex >= 0; jointIndex--)
                    {
                        int boneArrayIndex = fingerIndex * Hand.BONES + jointIndex;

                        if (_physicsHand.jointBones[boneArrayIndex].IsContacting || _physicsHand.jointBones[boneArrayIndex].IsGrasping || _physicsHand.jointBones[boneArrayIndex].IsObjectNearBone)
                        {
                            _fingerContacting[fingerIndex] = true;
                        }

                        float distanceCheck;
                        if (_wasGraspingBones[boneArrayIndex])
                        {
                            distanceCheck = fingerIndex == 0 ? Hand.CONTACT_THUMB_EXIT_DISTANCE : Hand.CONTACT_EXIT_DISTANCE;
                        }
                        else
                        {
                            distanceCheck = fingerIndex == 0 ? Hand.CONTACT_THUMB_ENTER_DISTANCE : Hand.CONTACT_ENTER_DISTANCE;
                        }

                        // If we haven't grasped the other joints then we're not going to successfully with the 0th.
                        if (_graspingFingers[fingerIndex] == -1 && jointIndex == 0)
                        {
                            if (!hasFingerGrasped)
                            {
                                _wasGraspingBones[boneArrayIndex] = false;
                            }
                            continue;
                        }

                        if (_graspingFingers[fingerIndex] != -1)
                        {
                            if (!_wasGraspingBones[boneArrayIndex])
                            {
                                _graspingXDrives[boneArrayIndex] = _physicsHand.jointBodies[boneArrayIndex].jointPosition[0] * Mathf.Rad2Deg;
                                _justGraspedBone[boneArrayIndex] = true;
                                _physicsHand.jointBodies[boneArrayIndex].angularVelocity = Vector3.zero;
                                _wasGraspingBones[boneArrayIndex] = true;
                            }
                        }
                        else if (_physicsHand.jointBones[boneArrayIndex].IsObjectNearBone && _physicsHand.jointBones[boneArrayIndex].ObjectDistance < distanceCheck)
                        {
                            if (_physicsHand.jointBones[boneArrayIndex].IsGrasping)
                            {
                                if (!_wasGraspingBones[boneArrayIndex])
                                {
                                    _graspingXDrives[boneArrayIndex] = _physicsHand.jointBodies[boneArrayIndex].jointPosition[0] * Mathf.Rad2Deg;
                                    _physicsHand.jointBodies[boneArrayIndex].angularVelocity = Vector3.zero;
                                    _justGraspedBone[boneArrayIndex] = true;
                                }
                                _graspingFingers[fingerIndex] = jointIndex;
                                _wasGraspingBones[boneArrayIndex] = true;
                            }
                            else if (_wasGraspingBones[boneArrayIndex])
                            {
                                _graspingFingers[fingerIndex] = jointIndex;
                            }
                        }
                        else
                        {
                            _wasGraspingBones[boneArrayIndex] = false;
                        }
                        if (_wasGraspingBones[boneArrayIndex])
                        {
                            hasFingerGrasped = true;
                            if (_physicsHand.jointBones[boneArrayIndex].IsObjectNearBone &&
                                (_physicsHand.jointBones[boneArrayIndex].ObjectDistance < _graspingFingerDistance[fingerIndex] || (_graspingFingerDistance[fingerIndex] == 1 && jointIndex == 0)))
                            {
                                _graspingFingerDistance[fingerIndex] = _physicsHand.jointBones[boneArrayIndex].ObjectDistance;
                            }
                        }
                    }
                }

                CacheComputedPositions();

        }

        private void CachePositions()
        {
            _physicsHand.oldPosition = _physicsHand.transform.position;
            _originalOldPosition = _originalLeapHand.PalmPosition;
            _physicsHand.previousDataPosition = _originalLeapHand.PalmPosition;
            _physicsHand.previousDataRotation = _originalLeapHand.Rotation;
        }

        private void CacheComputedPositions()
        {
            _physicsHand.computedPhysicsPosition = _physicsHand.transform.position;
            _physicsHand.computedPhysicsRotation = _physicsHand.transform.rotation;
            _physicsHand.computedHandDistance = Vector3.Distance(_physicsHand.previousDataPosition, _physicsHand.computedPhysicsPosition);
        }

        private void UpdateSettings()
        {
            if (_physicsProvider == null)
            {
                return;
            }
            _physicsHand.boneMass = _physicsProvider.PerBoneMass;
        }

        private void CalculateContacts()
        {
            _isContacting = IsAnyObjectInHandRadius(0.001f);
            _isCloseToObject = IsAnyObjectInHandRadius();
        }

        private void DelayedReset()
        {
            if (!_hasGenerated)
            {
                PhysicsHandsUtils.SetupHand(_physicsHand, _originalLeapHand, _physicsProvider.SolverIterations, _physicsProvider.SolverVelocityIterations);
                _physicsHand.gameObject.SetActive(true);
                _hasGenerated = true;
            }
            else
            {
                ResetPhysicsHand(true);
            }
        }

        private void CompleteReset()
        {
            PhysicsHandsUtils.UpdateIterations(ref _physicsHand, _physicsProvider.SolverIterations, _physicsProvider.SolverVelocityIterations);
            _leapHand.CopyFrom(_originalLeapHand);
            _timeOnReset = Time.time;
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

            IsTracked = false;

            ResetPhysicsHand(false);

            OnFinishPhysics?.Invoke();
            gameObject.SetActive(false);
            _physicsHand.gameObject.SetActive(false);
        }

        private void HandleTeleportingHands(bool bonesAreOverRotated)
        {
            _physicsHand.justGhosted = false;
            // Fix the hand if it gets into a bad situation by teleporting and holding in place until its bad velocities disappear
            if (Vector3.Distance(_originalOldPosition, _originalLeapHand.PalmPosition) > _physicsProvider.HandTeleportDistance ||
                bonesAreOverRotated ||
                DistanceFromDataHand > _physicsProvider.HandTeleportDistance && (IsGrasping ||
                IsCloseToObject))
            {
                ResetPhysicsHand(true);
                // Don't need to wait for the hand to reset as much here
                _teleportFrameCount = 5;

                _ghosted = true;
                _physicsHand.justGhosted = true;
            }

            if (Time.frameCount - _lastFrameTeleport >= _teleportFrameCount && _ghosted && !IsCloseToObject)
            {
                ChangeHandLayer(_physicsProvider.HandsLayer);

                _ghosted = false;
            }
        }

        private void HandleIgnoredObjects()
        {
            if (_ignoredData.Count > 0)
            {
                for (int i = 0; i < _ignoredData.Count; i++)
                {
                    // Handle destroyed objects
                    if (_ignoredData[i].rigid == null)
                    {
                        _ignoredData.RemoveAt(i);
                        continue;
                    }

                    if (_ignoredData[i].timeout >= 0)
                    {
                        _ignoredData[i].timeout -= Time.fixedDeltaTime;
                    }

                    if (_ignoredData[i].timeout <= 0 && !IsObjectInHandRadius(_ignoredData[i].rigid, _ignoredData[i].radius))
                    {
                        TogglePhysicsIgnore(_ignoredData[i].rigid, false);
                        i--;
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

        #region External Factors Functions

        /// <summary>
        /// Used to make sure that the hand is not going to make contact with any object within the scene. Adjusting extraRadius will inflate the joints. This prevents hands from attacking objects when they swap back to contacting.
        /// </summary>
        public bool IsAnyObjectInHandRadius(float extraRadius = 0.005f)
        {
            if (IsAnyObjectInBoneRadius(_physicsHand.palmBone, extraRadius))
            {
                return true;
            }

            foreach (var bone in _physicsHand.jointBones)
            {
                if (IsAnyObjectInBoneRadius(bone, extraRadius))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Used to make sure that a physicsBone is not going to make contact with any object within the scene. Adjusting extraRadius will inflate the joints.
        /// </summary>
        public bool IsAnyObjectInBoneRadius(PhysicsBone physicsBone, float extraRadius = 0.005f)
        {
            int overlappingColliders;
            if (physicsBone.Finger == 5)
            {
                overlappingColliders = PhysExts.OverlapBoxNonAllocOffset((BoxCollider)physicsBone.Collider, Vector3.zero, _colliderCache, _layerMask, QueryTriggerInteraction.Ignore, extraRadius);
            }
            else
            {
                overlappingColliders = PhysExts.OverlapCapsuleNonAllocOffset((CapsuleCollider)physicsBone.Collider, Vector3.zero, _colliderCache, _layerMask, QueryTriggerInteraction.Ignore, extraRadius);
            }

            for (int i = 0; i < overlappingColliders; i++)
            {
                if (WillColliderAffectHand(_colliderCache[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Used to ensure that a leapBone is not going to be in a problematic state. This can be used to ensure that a desired location is clear of an object.
        /// </summary>
        public bool IsAnyObjectInBoneRadius(Bone leapBone, float extraRadius = 0.005f)
        {
            int overlappingColliders = Physics.OverlapCapsuleNonAlloc(leapBone.PrevJoint, leapBone.NextJoint, (leapBone.Width / 2f) + extraRadius, _colliderCache, _layerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < overlappingColliders; i++)
            {
                if (WillColliderAffectHand(_colliderCache[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private bool WillColliderAffectHand(Collider collider)
        {
            if (collider.attachedRigidbody != null && collider.attachedRigidbody.TryGetComponent<PhysicsIgnoreHelpers>(out var temp) && temp.DisableHandCollisions)
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
        /// Checks to see whether the hand will be in contact with a specified object. Radius can be used to inflate the bones.
        /// </summary>
        public bool IsObjectInHandRadius(Rigidbody rigid, float extraRadius = 0f)
        {
            if (rigid == null)
                return false;

            if (IsObjectInBoneRadius(rigid, _physicsHand.palmBone, extraRadius))
            {
                return true;
            }

            foreach (var bone in _physicsHand.jointBones)
            {
                if (IsObjectInBoneRadius(rigid, bone, extraRadius))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks to see whether a specific physicsBone will be in contact with a specified object. Radius can be used to inflate the physicsBone.
        /// </summary>
        public bool IsObjectInBoneRadius(Rigidbody rigid, PhysicsBone bone, float extraRadius = 0f)
        {
            int overlappingColliders;
            if (bone.Finger == 5)
            {
                overlappingColliders = PhysExts.OverlapBoxNonAllocOffset((BoxCollider)bone.Collider, Vector3.zero, _colliderCache, _layerMask, QueryTriggerInteraction.Ignore, extraRadius);
            }
            else
            {
                overlappingColliders = PhysExts.OverlapCapsuleNonAllocOffset((CapsuleCollider)bone.Collider, Vector3.zero, _colliderCache, _layerMask, QueryTriggerInteraction.Ignore, extraRadius);
            }
            for (int i = 0; i < overlappingColliders; i++)
            {
                if (_colliderCache[i].attachedRigidbody == rigid)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Have the bones been forced beyond acceptable rotation amounts by external forces?
        /// </summary>
        private bool AreBonesRotatedBeyondThreshold(float eulerThreshold = 20f)
        {
            if (IsGrasping)
            {
                return false;
            }

            for (int fingerIndex = 0; fingerIndex < Hand.FINGERS; fingerIndex++)
            {
                for (int jointIndex = 1; jointIndex < Hand.BONES; jointIndex++)
                {
                    int boneArrayIndex = fingerIndex * Hand.BONES + jointIndex;

                    // Skip finger if a bone's contacting
                    if (_physicsHand.jointBones[boneArrayIndex].IsContacting)
                    {
                        break;
                    }

                    ArticulationBody body = _physicsHand.jointBodies[boneArrayIndex];
                    float angle = Mathf.Repeat(body.transform.localRotation.eulerAngles.x + 180, 360) - 180;
                    if (angle < body.xDrive.lowerLimit - eulerThreshold)
                    {
                        return true;
                    }

                    if (angle > body.xDrive.upperLimit + eulerThreshold)
                    {
                        return true;
                    }

                    // If the bone's meant to be pretty flat
                    float delta = Mathf.DeltaAngle(angle, body.xDrive.target);
                    if (Mathf.Abs(body.xDrive.target) < eulerThreshold / 2f && delta > eulerThreshold)
                    {
                        // We are over rotated, add a frame to the frame count
                        _physicsHand.overRotationFrameCount[boneArrayIndex]++;
                        if (_physicsHand.overRotationFrameCount[boneArrayIndex] > 10)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        // We were over rotated, but no longer are, remove a frame from the frame count
                        _physicsHand.overRotationFrameCount[boneArrayIndex] = Mathf.Clamp(_physicsHand.overRotationFrameCount[boneArrayIndex] - 1, 0, 10);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Disables all collisions between a rigidbody and hand. Will automatically handle all colliders on the rigidbody. Timeout lets you specify a minimum time to ignore collisions for.
        /// </summary>
        public void IgnoreCollision(Rigidbody rigid, float timeout = 0, float radius = 0)
        {
            TogglePhysicsIgnore(rigid, true, timeout, radius);
        }

        private void TogglePhysicsIgnore(Rigidbody rigid, bool ignore, float timeout = 0, float radius = 0)
        {
            // If the rigid has been destroyed we can't do anything
            if (rigid == null)
            {
                return;
            }
            Collider[] colliders = rigid.GetComponentsInChildren<Collider>(true);
            foreach (var collider in colliders)
            {
                Physics.IgnoreCollision(collider, _physicsHand.palmCollider, ignore);
                foreach (var boneCollider in _physicsHand.jointColliders)
                {
                    Physics.IgnoreCollision(collider, boneCollider, ignore);
                }
            }
            int ind = _ignoredData.FindIndex(x => x.rigid == rigid);
            if (ignore)
            {
                if (ind == -1)
                {
                    _ignoredData.Add(new IgnoreData(rigid, colliders) { timeout = timeout, radius = radius });
                }
                else
                {
                    _ignoredData[ind].timeout = timeout;
                    _ignoredData[ind].radius = radius;
                }
            }
            else
            {
                if (ind != -1)
                {
                    _ignoredData.RemoveAt(ind);
                }
            }
        }

        #endregion
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            for (int fingerIndex = 0; fingerIndex < Hand.FINGERS; fingerIndex++)
            {
                for (int jointIndex = Hand.BONES - 1; jointIndex >= 0; jointIndex--)
                {
                    int boneArrayIndex = fingerIndex * Hand.BONES + jointIndex;
                    Gizmos.color = _wasGraspingBones[boneArrayIndex] ? Color.green : Color.red;
                    Gizmos.DrawSphere(_physicsHand.jointBones[boneArrayIndex].Collider.bounds.center, 0.005f);
                }
            }
        }
    }
}