/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Attributes;
using Leap.Unity.Interaction.Internal;

using Leap.Unity.RuntimeGizmos;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction
{
    public enum HandDataMode { PlayerLeft, PlayerRight, Custom }

    /// <summary>
    /// This class allows you to use hand tracking as the controller for the interaction engine.
    /// </summary>
    [DisallowMultipleComponent]
    public class InteractionHand : InteractionController
    {
        #region Inspector

        [SerializeField]
        private LeapProvider _leapProvider;

        [Header("Hand Configuration")]

        [Tooltip("Should the data for the underlying Leap hand come from the player's left "
               + "hand or their right hand? Alternatively, you can set this mode to Custom "
               + "to specify accessor functions manually via script (recommended for advanced "
               + "users only).")]
        [SerializeField, EditTimeOnly]
        private HandDataMode _handDataMode;

        /// <summary>
        /// Should the data for the underlying Leap hand come from the player's left hand or their right hand? Alternatively, you can set this mode to Custom to specify accessor functions manually via script (recommended for advanced users only).
        /// </summary>
        public HandDataMode handDataMode
        {
            get { return _handDataMode; }
            set
            {
                // TODO: Do validation if this is modified!
                _handDataMode = value;
            }
        }

        private bool _indexOnly = false;
        /// <summary>
        /// //An option to just Initialise the index finger for interaction
        /// </summary>
        public bool OnlyInitialiseIndexFinger
        {
            get { return _indexOnly; }
            set
            {
                _indexOnly = value;
                _wasContactInitialized = false;
            }
        }

        /// <summary>
        /// Set slots to true to consider the corresponding finger's fingertip for primary
        /// hover checks. 0 is the thumb, 1 is the index finger, etc. Generally speaking,
        /// enable the fingertips you'd like users to be able to use to choose and push a
        /// button, but keep in mind you pay distance check costs for each fingertip enabled!
        /// </summary>
        public bool[] enabledPrimaryHoverFingertips = new bool[5] { true, true, true, false, false };

        #endregion

        #region Hand Data
        /// <summary>
        /// If the hand data mode for this InteractionHand is set to Custom, you must also
        /// manually specify the provider from which to retrieve Leap frames containing
        /// hand data.
        /// </summary>
        public LeapProvider leapProvider
        {
            get { return _leapProvider; }
            set
            {
                if (_leapProvider != null && Application.isPlaying)
                {
                    _leapProvider.OnFixedFrame -= onProviderFixedFrame;
                }

                _leapProvider = value;

                if (_leapProvider != null && Application.isPlaying)
                {
                    _leapProvider.OnFixedFrame += onProviderFixedFrame;
                }
            }
        }

        private Func<Leap.Frame, Leap.Hand> _handAccessorFunc;
        /// <summary>
        /// If the hand data mode for this InteractionHand is set to Custom, you must
        /// manually specify how this InteractionHand should retrieve a specific Hand data
        /// object from a Leap frame.
        /// </summary>
        public Func<Leap.Frame, Leap.Hand> handAccessorFunc
        {
            get { return _handAccessorFunc; }
            set { _handAccessorFunc = value; }
        }

        /// <summary>
        /// A copy of the latest tracked hand data; never null, never warped.
        /// </summary>
        private Hand _handData = new Hand();

        /// <summary>
        /// An unwarped copy of _handData (if unwarping is necessary; otherwise
        /// identical). Only relevant when using the Leap Graphical Renderer to create
        /// curved user interfaces.
        /// </summary>
        private Hand _unwarpedHandData = new Hand();

        /// <summary>
        /// Will be null when not tracked, otherwise contains the same data as _handData.
        /// </summary>
        private Hand _hand;

        #endregion

        #region Unity Events

        protected override void Reset()
        {
            base.Reset();

            enabledPrimaryHoverFingertips = new bool[] { true, true, true, false, false };
        }

        protected override void Start()
        {
            base.Start();

            // Check manual configuration if data mode is custom.
            if (handDataMode == HandDataMode.Custom)
            {
                if (leapProvider == null)
                {
                    Debug.LogError("handDataMode is set to Custom, but no provider is set! "
                                 + "Please add a custom script that will configure the correct "
                                 + "LeapProvider for this InteractionHand before its Start() is "
                                 + "called, or set the handDataMode to a value other than Custom.",
                                 this);
                    return;
                }
                else if (handAccessorFunc == null)
                {
                    Debug.LogError("handDataMode is set to Custom, but no handAccessorFunc has "
                                 + "been set! Please add a custom script that will configure the "
                                 + "hand accessor function that will convert Leap frames into "
                                 + "Leap hand data for this InteractionHand before its Start() "
                                 + "is called, or set the handDataMode to a value other than "
                                 + "Custom.", this);
                    return;
                }
            }
            else
            { // Otherwise, configure automatically.
                if (leapProvider == null)
                {
                    leapProvider = FindObjectOfType<LeapProvider>();

                    if (leapProvider == null)
                    {
                        Debug.LogError("No LeapServiceProvider was found in your scene! Please "
                                     + "make sure you have a LeapServiceProvider if you intend to "
                                     + "use Leap hands in your scene.", this);
                        return;
                    }
                }

                if (handAccessorFunc == null)
                {
                    if (handDataMode == HandDataMode.PlayerLeft)
                    {
                        handAccessorFunc = (frame) => frame.GetHand(Chirality.Left);
                    }
                    else
                    {
                        handAccessorFunc = (frame) => frame.GetHand(Chirality.Right);
                    }
                }
            }

            leapProvider.OnFixedFrame -= onProviderFixedFrame; // avoid double-subscribe
            leapProvider.OnFixedFrame += onProviderFixedFrame;

            // Set up hover point Transform for the palm.
            Transform palmTransform = new GameObject("Palm Transform").transform;
            palmTransform.parent = this.transform;
            _backingHoverPointTransform = palmTransform;

            // Set up primary hover point Transforms for the fingertips. We'll only use
            // some of them, depending on user settings.
            for (int i = 0; i < 5; i++)
            {
                Transform fingertipTransform = new GameObject("Fingertip Transform").transform;
                fingertipTransform.parent = this.transform;
                _backingFingertipTransforms.Add(fingertipTransform);
                _fingertipTransforms.Add(null);
            }
        }

        private void OnDestroy()
        {
            if (_leapProvider != null)
            {
                _leapProvider.OnFixedFrame -= onProviderFixedFrame;
            }
        }

        private void onProviderFixedFrame(Leap.Frame frame)
        {
            _hand = handAccessorFunc(frame);

            if (_hand != null)
            {
                _handData.CopyFrom(_hand);
                _unwarpedHandData.CopyFrom(_handData);

                refreshPointDataFromHand();
                _lastCustomHandWasLeft = _unwarpedHandData.IsLeft;
            }

        }

        #endregion

        #region General InteractionController Implementation

        /// <summary>
        /// Gets whether the underlying Leap hand is currently tracked.
        /// </summary>
        public override bool isTracked { get { return _hand != null; } }

        /// <summary>
        /// Gets whether the underlying Leap hand is currently being moved in worldspace.
        /// </summary>
        public override bool isBeingMoved { get { return isTracked; } }

        /// <summary>
        /// Gets the last tracked state of the Leap hand.
        /// 
        /// Note for those using the Leap Graphical Renderer: If the hand required warping
        /// due to the nearby presence of an object in warped (curved) space, this will
        /// return the hand as warped from that object's curved space into the rectilinear
        /// space containing its colliders. This is only relevant if you are using the Leap
        /// Graphical Renderer to render curved, interactive objects.
        /// </summary>
        public Hand leapHand { get { return _unwarpedHandData; } }

        private bool _lastCustomHandWasLeft = false;
        /// <summary>
        /// Gets whether the underlying tracked Leap hand is a left hand.
        /// </summary>
        public override bool isLeft
        {
            get
            {
                switch (handDataMode)
                {
                    case HandDataMode.PlayerLeft:
                        return true;
                    case HandDataMode.PlayerRight:
                        return false;
                    case HandDataMode.Custom:
                    default:
                        return _lastCustomHandWasLeft;
                }
            }
        }

        /// <summary>
        /// Gets the last-tracked position of the underlying Leap hand.
        /// </summary>
        public override Vector3 position
        {
            get { return _handData.PalmPosition; }
        }

        /// <summary>
        /// Gets the last-tracked rotation of the underlying Leap hand.
        /// </summary>
        public override Quaternion rotation
        {
            get { return _handData.Rotation; }
        }

        /// <summary>
        /// Gets the velocity of the underlying tracked Leap hand.
        /// </summary>
        public override Vector3 velocity
        {
            get { return isTracked ? leapHand.PalmVelocity : Vector3.zero; }
        }

        /// <summary>
        /// Gets the controller type of this InteractionControllerBase. InteractionHands
        /// are Interaction Engine controllers implemented over Leap hands.
        /// </summary>
        public override ControllerType controllerType
        {
            get { return ControllerType.Hand; }
        }

        /// <summary>
        /// Returns this InteractionHand object. This property will be null if the
        /// InteractionControllerBase is not ControllerType.Hand.
        /// </summary>
        public override InteractionHand intHand
        {
            get { return this; }
        }

        protected override void onObjectUnregistered(IInteractionBehaviour intObj)
        {
            grabClassifier.UnregisterInteractionBehaviour(intObj);
        }

        protected override void fixedUpdateController()
        {
            // Transform the hand ahead if the manager is in a moving reference frame.
            if (manager.hasMovingFrameOfReference)
            {
                Vector3 transformAheadPosition;
                Quaternion transformAheadRotation;
                manager.TransformAheadByFixedUpdate(_unwarpedHandData.PalmPosition,
                                                    _unwarpedHandData.Rotation,
                                                    out transformAheadPosition,
                                                    out transformAheadRotation);
                _unwarpedHandData.SetTransform(transformAheadPosition, transformAheadRotation);
            }
        }

        #endregion

        #region Hovering Controller Implementation

        private Transform _backingHoverPointTransform = null;

        /// <summary>
        /// The point in space where a hover is happening for this interacion hand
        /// </summary>
        public override Vector3 hoverPoint
        {
            get
            {
                if (_backingHoverPointTransform == null)
                {
                    return leapHand.PalmPosition;
                }
                else
                {
                    return _backingHoverPointTransform.position;
                }
            }
        }

        private List<Transform> _backingFingertipTransforms = new List<Transform>();
        private List<Transform> _fingertipTransforms = new List<Transform>();
        protected override List<Transform> _primaryHoverPoints
        {
            get
            {
                return _fingertipTransforms;
            }
        }

        private void refreshPointDataFromHand()
        {
            refreshHoverPoint();
            refreshPrimaryHoverPoints();
            refreshGraspManipulatorPoints();
        }

        private void refreshHoverPoint()
        {
            _backingHoverPointTransform.position = leapHand.PalmPosition;
            _backingHoverPointTransform.rotation = leapHand.Rotation;
        }

        private void refreshPrimaryHoverPoints()
        {
            for (int i = 0; i < enabledPrimaryHoverFingertips.Length; i++)
            {
                if (enabledPrimaryHoverFingertips[i])
                {
                    _fingertipTransforms[i] = _backingFingertipTransforms[i];

                    Finger finger = leapHand.Fingers[i];
                    _fingertipTransforms[i].position = finger.TipPosition;
                    _fingertipTransforms[i].rotation = finger.bones[3].Rotation;
                }
                else
                {
                    _fingertipTransforms[i] = null;
                }
            }
        }

        #endregion

        #region Contact Controller Implementation

        private const int NUM_FINGERS = 5;
        private const int BONES_PER_FINGER = 3;

        private ContactBone[] _contactBones;

        /// <summary>
        /// Get an array of Contact bones that are used to calculate interactions
        /// </summary>
        public override ContactBone[] contactBones
        {
            get { return _contactBones; }
        }

        private GameObject _contactBoneParent;
        protected override GameObject contactBoneParent
        {
            get { return _contactBoneParent; }
        }

        private delegate void BoneMapFunc(Leap.Hand hand, out Vector3 targetPosition,
                                                          out Quaternion targetRotation);
        private BoneMapFunc[] _handContactBoneMapFunctions;

        protected override void getColliderBoneTargetPositionRotation(int contactBoneIndex,
                                                                      out Vector3 targetPosition,
                                                                      out Quaternion targetRotation)
        {
            _handContactBoneMapFunctions[contactBoneIndex](_unwarpedHandData,
                                                           out targetPosition,
                                                           out targetRotation);
        }

        protected override bool initContact()
        {
            if (!isTracked)
            {
                _wasContactInitialized = false;
                return false;
            }
            initContactBoneContainer();
            initContactBones();

            return true;
        }

        protected override void onPreEnableSoftContact()
        {
            resetContactBoneJoints();
        }

        protected override void onPostDisableSoftContact()
        {
            if (isTracked) resetContactBoneJoints();
        }

        #region Contact Bone Management

        private void initContactBoneContainer()
        {
            if (_contactBoneParent != null)
            {
                Destroy(_contactBoneParent);
            }
            string name = (_unwarpedHandData.IsLeft ? "Left" : "Right") + " Interaction Hand Contact Bones";
            _contactBoneParent = new GameObject(name);
        }

        private void initContactBones()
        {
            if (_contactBones != null)
            {
                for (int i = 0; i < _contactBones.Length; i++)
                {
                    Destroy(_contactBones[i]);
                }
            }

            _contactBones = new ContactBone[NUM_FINGERS * BONES_PER_FINGER + 1];
            _handContactBoneMapFunctions = new BoneMapFunc[NUM_FINGERS * BONES_PER_FINGER + 1];

            // Finger bones
            for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++)
            {
                for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++)
                {
                    GameObject contactBoneObj = new GameObject("Contact Fingerbone", typeof(CapsuleCollider), typeof(Rigidbody), typeof(ContactBone));
                    contactBoneObj.layer = manager.contactBoneLayer;

                    Bone bone = _unwarpedHandData.Fingers[fingerIndex]
                                                 .Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
                    int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;
                    contactBoneObj.transform.position = bone.Center;
                    contactBoneObj.transform.rotation = bone.Rotation;

                    // Remember the method we used to calculate this bone position from
                    // a Leap Hand for later.
                    int fingerIndexCopy = fingerIndex;
                    int jointIndexCopy = jointIndex;
                    _handContactBoneMapFunctions[boneArrayIndex] = (Leap.Hand hand,
                                                                    out Vector3 targetPosition,
                                                                    out Quaternion targetRotation) =>
                    {
                        Bone theBone = hand.Fingers[fingerIndexCopy].Bone((Bone.BoneType)(jointIndexCopy + 1));
                        targetPosition = theBone.Center;
                        targetRotation = theBone.Rotation;
                    };

                    CapsuleCollider capsule = contactBoneObj.GetComponent<CapsuleCollider>();
                    capsule.direction = 2;
                    capsule.radius = bone.Width * 0.5f;
                    capsule.height = bone.Length + bone.Width;
                    capsule.material = defaultContactBoneMaterial;
                    capsule.contactOffset = 0.001f;

                    if (OnlyInitialiseIndexFinger)
                    {
                        capsule.enabled = (fingerIndex == 1 && (jointIndex == 2 || jointIndex == 1));
                    }
                    ContactBone contactBone = initContactBone(bone, contactBoneObj, boneArrayIndex, capsule);

                    contactBone.lastTargetPosition = bone.Center;
                }
            }

            // Palm bone
            {
                // Palm is attached to the third metacarpal and derived from it.
                GameObject contactBoneObj = new GameObject("Contact Palm Bone", typeof(BoxCollider), typeof(Rigidbody), typeof(ContactBone));

                Bone bone = _unwarpedHandData.Fingers[(int)Finger.FingerType.TYPE_MIDDLE].Bone(Bone.BoneType.TYPE_METACARPAL);
                int boneArrayIndex = NUM_FINGERS * BONES_PER_FINGER;
                contactBoneObj.transform.position = _unwarpedHandData.PalmPosition;
                contactBoneObj.transform.rotation = _unwarpedHandData.Rotation;

                // Remember the method we used to calculate the palm from a Leap Hand for later.
                _handContactBoneMapFunctions[boneArrayIndex] = (Leap.Hand hand,
                                                                out Vector3 targetPosition,
                                                                out Quaternion targetRotation) =>
                {
                    targetPosition = hand.PalmPosition;
                    targetRotation = hand.Rotation;
                };

                BoxCollider box = contactBoneObj.GetComponent<BoxCollider>();
                box.center = new Vector3(_unwarpedHandData.IsLeft ? -0.005f : 0.005f, bone.Width * -0.3f, -0.01f);
                box.size = new Vector3(bone.Length, bone.Width, bone.Length);
                box.material = defaultContactBoneMaterial;
                box.contactOffset = 0.001f;

                if (OnlyInitialiseIndexFinger)
                {
                    box.enabled = false;
                }
                initContactBone(null, contactBoneObj, boneArrayIndex, box);
            }

            // Constrain the bones to each other to prevent separation.
            addContactBoneJoints();
        }

        private ContactBone initContactBone(Leap.Bone bone, GameObject contactBoneObj, int boneArrayIndex, Collider boneCollider)
        {
            contactBoneObj.layer = _contactBoneParent.gameObject.layer;
            contactBoneObj.transform.localScale = Vector3.one;

            ContactBone contactBone = contactBoneObj.GetComponent<ContactBone>();
            contactBone.collider = boneCollider;
            contactBone.interactionController = this;
            contactBone.interactionHand = this;
            _contactBones[boneArrayIndex] = contactBone;

            Transform capsuleTransform = contactBoneObj.transform;
            capsuleTransform.SetParent(_contactBoneParent.transform, false);

            Rigidbody body = contactBoneObj.GetComponent<Rigidbody>();
            body.freezeRotation = true;
            contactBone.rigidbody = body;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // TODO: Allow different collision detection modes as an optimization.

            body.mass = 0.1f;
            body.position = bone != null ? bone.Center
                                         : _unwarpedHandData.PalmPosition;
            body.rotation = bone != null ? bone.Rotation
                                         : _unwarpedHandData.Rotation;
            contactBone.lastTargetPosition = bone != null ? bone.Center
                                                  : _unwarpedHandData.PalmPosition;

            return contactBone;
        }

        private void addContactBoneJoints()
        {
            for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++)
            {
                for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++)
                {
                    Bone bone = _unwarpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
                    int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;

                    FixedJoint joint = _contactBones[boneArrayIndex].gameObject.AddComponent<FixedJoint>();
                    joint.autoConfigureConnectedAnchor = false;
                    if (jointIndex != 0)
                    {
                        Bone prevBone = _unwarpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
                        joint.connectedBody = _contactBones[boneArrayIndex - 1].rigidbody;
                        joint.anchor = Vector3.back * bone.Length / 2f;
                        joint.connectedAnchor = Vector3.forward * prevBone.Length / 2f;
                        _contactBones[boneArrayIndex].joint = joint;
                    }
                    else
                    {
                        joint.connectedBody = _contactBones[NUM_FINGERS * BONES_PER_FINGER].rigidbody;
                        joint.anchor = Vector3.back * bone.Length / 2f;
                        joint.connectedAnchor = _contactBones[NUM_FINGERS * BONES_PER_FINGER].transform.InverseTransformPoint(bone.PrevJoint);
                        _contactBones[boneArrayIndex].metacarpalJoint = joint;
                    }
                }
            }
        }

        /// <summary> Reconnects and resets all the joints in the hand. </summary>
        private void resetContactBoneJoints()
        {
            // If contact bones array is null, there's nothing to reset. This can happen if
            // the controller is disabled before it had a chance to initialize contact.
            if (_contactBones == null) return;

            // If the palm contact bone is null, we can't reset bone joints.
            if (_contactBones[NUM_FINGERS * BONES_PER_FINGER] == null) return;

            _contactBones[NUM_FINGERS * BONES_PER_FINGER].transform.position = _unwarpedHandData.PalmPosition;
            _contactBones[NUM_FINGERS * BONES_PER_FINGER].transform.rotation = _unwarpedHandData.Rotation;
            _contactBones[NUM_FINGERS * BONES_PER_FINGER].rigidbody.velocity = Vector3.zero;
            _contactBones[NUM_FINGERS * BONES_PER_FINGER].rigidbody.angularVelocity = Vector3.zero;

            for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++)
            {
                for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++)
                {
                    Bone bone = _unwarpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
                    int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;

                    _contactBones[boneArrayIndex].transform.position = bone.Center;
                    _contactBones[boneArrayIndex].transform.rotation = bone.Rotation;
                    _contactBones[boneArrayIndex].rigidbody.position = bone.Center;
                    _contactBones[boneArrayIndex].rigidbody.rotation = bone.Rotation;
                    _contactBones[boneArrayIndex].rigidbody.velocity = Vector3.zero;
                    _contactBones[boneArrayIndex].rigidbody.angularVelocity = Vector3.zero;

                    if (jointIndex != 0 && _contactBones[boneArrayIndex].joint != null)
                    {
                        Bone prevBone = _unwarpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
                        _contactBones[boneArrayIndex].joint.connectedBody = _contactBones[boneArrayIndex - 1].rigidbody;
                        _contactBones[boneArrayIndex].joint.anchor = Vector3.back * bone.Length / 2f;
                        _contactBones[boneArrayIndex].joint.connectedAnchor = Vector3.forward * prevBone.Length / 2f;
                    }
                    else if (_contactBones[boneArrayIndex].metacarpalJoint != null)
                    {
                        _contactBones[boneArrayIndex].metacarpalJoint.connectedBody = _contactBones[NUM_FINGERS * BONES_PER_FINGER].rigidbody;
                        _contactBones[boneArrayIndex].metacarpalJoint.anchor = Vector3.back * bone.Length / 2f;
                        _contactBones[boneArrayIndex].metacarpalJoint.connectedAnchor = _contactBones[NUM_FINGERS * BONES_PER_FINGER].transform
                                                                                        .InverseTransformPoint(bone.PrevJoint);
                    }
                }
            }
        }

        /// <summary>
        /// A utility function that sets a Hand object's bones based on this InteractionHand.
        /// Can be used to display a graphical hand that matches the physical one.
        /// </summary>
        public void FillBones(Hand inHand)
        {
            if (softContactEnabled) { return; }
            if (Application.isPlaying && _contactBones.Length == NUM_FINGERS * BONES_PER_FINGER + 1)
            {
                Vector3 elbowPos = inHand.Arm.ElbowPosition;
                inHand.SetTransform(_contactBones[NUM_FINGERS * BONES_PER_FINGER].rigidbody.position, _contactBones[NUM_FINGERS * BONES_PER_FINGER].rigidbody.rotation);

                for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++)
                {
                    for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++)
                    {
                        Bone bone = inHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1);
                        int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;
                        Vector3 displacement = _contactBones[boneArrayIndex].rigidbody.position - bone.Center;
                        bone.Center += displacement;
                        bone.PrevJoint += displacement;
                        bone.NextJoint += displacement;
                        bone.Rotation = _contactBones[boneArrayIndex].rigidbody.rotation;
                    }
                }

                inHand.Arm.PrevJoint = elbowPos;
                inHand.Arm.Direction = (inHand.Arm.PrevJoint - inHand.Arm.NextJoint).normalized;
                inHand.Arm.Center = (inHand.Arm.PrevJoint + inHand.Arm.NextJoint) * 0.5f;
            }
        }

        #endregion

        #endregion

        #region Grasp Controller Implementation

        private List<Vector3> _graspManipulatorPoints = new List<Vector3>();
        /// <summary>
        /// Get the list of graspManipulatorPoints from this interaction hand
        /// </summary>
        public override List<Vector3> graspManipulatorPoints
        {
            get
            {
                return _graspManipulatorPoints;
            }
        }

        private void refreshGraspManipulatorPoints()
        {
            int bufferIndex = 0;
            for (int i = 0; i < NUM_FINGERS; i++)
            {
                for (int boneIdx = 0; boneIdx < 2; boneIdx++)
                {
                    // Update or add knuckle-joint and first-finger-bone positions as the grasp
                    // manipulator points for this Hand.

                    Vector3 point = leapHand.Fingers[i].bones[boneIdx].NextJoint;

                    if (_graspManipulatorPoints.Count - 1 < bufferIndex)
                    {
                        _graspManipulatorPoints.Add(point);
                    }
                    else
                    {
                        _graspManipulatorPoints[bufferIndex] = point;
                    }
                    bufferIndex += 1;
                }
            }
        }

        private HeuristicGrabClassifier _grabClassifier;

        /// <summary>
        /// Handles logic determining whether a hand has grabbed or released an interaction object.
        /// </summary>
        public HeuristicGrabClassifier grabClassifier
        {
            get
            {
                if (_grabClassifier == null) _grabClassifier = new HeuristicGrabClassifier(this);
                return _grabClassifier;
            }
        }

        private Vector3[] _fingertipPositionsBuffer = new Vector3[5];

        /// <summary>
        /// Returns approximately where the controller is grasping the currently-grasped
        /// InteractionBehaviour. Specifically, returns the average position of all grasping
        /// fingertips of the InteractionHand.
        /// 
        /// This method will print an error if the hand is not currently grasping an object.
        /// </summary>
        public override Vector3 GetGraspPoint()
        {
            if (!isGraspingObject)
            {
                Debug.LogError("Tried to get grasp point of InteractionHand, but it is not "
                             + "currently grasping an object.", this);
                return leapHand.PalmPosition;
            }

            int numGraspingFingertips = 0;
            _grabClassifier.GetGraspingFingertipPositions(graspedObject, _fingertipPositionsBuffer, out numGraspingFingertips);
            if (numGraspingFingertips > 0)
            {
                Vector3 sum = Vector3.zero;
                for (int i = 0; i < numGraspingFingertips; i++)
                {
                    sum += _fingertipPositionsBuffer[i];
                }
                return sum / numGraspingFingertips;
            }
            else
            {
                return leapHand.PalmPosition;
            }
        }

        /// <summary>
        /// Attempts to manually initiate a grasp on the argument interaction object. A grasp
        /// will only begin if a finger and thumb are both in contact with the interaction
        /// object. If this method successfully initiates a grasp, it will return true,
        /// otherwise it will return false.
        /// </summary>
        protected override bool checkShouldGraspAtemporal(IInteractionBehaviour intObj)
        {
            if (grabClassifier.TryGrasp(intObj, leapHand))
            {
                var tempControllers = Pool<List<InteractionController>>.Spawn();
                try
                {
                    tempControllers.Add(this);
                    intObj.BeginGrasp(tempControllers);
                    return true;
                }
                finally
                {
                    tempControllers.Clear();
                    Pool<List<InteractionController>>.Recycle(tempControllers);
                }
            }

            return false;
        }

        /// <summary>
        /// Can be used to swap the current graspedObject for another interaction behaviour
        /// </summary>
        public override void SwapGrasp(IInteractionBehaviour replacement)
        {
            var original = graspedObject;

            base.SwapGrasp(replacement);

            grabClassifier.SwapClassifierState(original, replacement);
        }

        protected override void fixedUpdateGraspingState()
        {
            //Feed Camera Transform in for Projective Grabbing Hack (details inside)
            grabClassifier.FixedUpdateClassifierHandState();
        }

        protected override void onGraspedObjectForciblyReleased(IInteractionBehaviour objectToBeReleased)
        {
            grabClassifier.NotifyGraspForciblyReleased(objectToBeReleased);
        }

        protected override bool checkShouldRelease(out IInteractionBehaviour objectToRelease)
        {
            return grabClassifier.FixedUpdateClassifierRelease(out objectToRelease);
        }

        protected override bool checkShouldGrasp(out IInteractionBehaviour objectToGrasp)
        {
            return grabClassifier.FixedUpdateClassifierGrasp(out objectToGrasp);
        }

        #endregion

        #region Gizmos

#if UNITY_EDITOR

        private Leap.Hand _testHand = null;

        /// <summary>
        /// Draw gizmos during runtime to help with debugging
        /// </summary>
        public override void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer)
        {
            if (Application.isPlaying)
            {
                base.OnDrawRuntimeGizmos(drawer);
            }
            else
            {
                var provider = leapProvider;
                if (provider == null)
                {
                    provider = FindObjectOfType<LeapProvider>();
                }

                if (_testHand == null && provider != null)
                {
                    _testHand = provider.MakeTestHand(this.isLeft);
                }

                // Hover Point
                _unwarpedHandData = _testHand; // hoverPoint is driven by this backing variable
                drawHoverPoint(drawer, hoverPoint);

                // Primary Hover Points
                for (int i = 0; i < NUM_FINGERS; i++)
                {
                    if (enabledPrimaryHoverFingertips[i])
                    {
                        drawPrimaryHoverPoint(drawer, _testHand.Fingers[i].TipPosition);
                    }
                }
            }
        }

#endif

        #endregion

    }
}