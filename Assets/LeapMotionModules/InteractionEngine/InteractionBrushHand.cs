using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity
{
    /** Collision brushes */
    public class InteractionBrushHand : IHandModel
    {
        private const int N_FINGERS = 5;
        private const int N_ACTIVE_BONES = 3;

        private Rigidbody palmBody;
        private Rigidbody[] _capsuleBodies;
        private ConfigurableJoint[] _ConfigurableJoints;
        private Quaternion[] _origPalmToJointRotation;
        private Vector3 _lastPalmPosition;
        private Vector3[] _lastPositions;
        private Hand hand_;

        public override ModelType HandModelType
        {
            get { return ModelType.Physics; }
        }

        [SerializeField]
        private Chirality handedness;
        public override Chirality Handedness
        {
            get { return handedness; }
        }

        [SerializeField]
        [Tooltip("The mass of each finger bone; the palm will be 3x this.")]
        private float _perBoneMass = 3.0f;

        [SerializeField]
        [Tooltip("The maximum velocity that is applied when setting the velocity of the bodies.")]
        private float _maxVelocity = 2.0f;

        [SerializeField]
        [Tooltip("Which collision mode the hand uses.")]
        private CollisionDetectionMode _collisionDetection = CollisionDetectionMode.ContinuousDynamic;

        [SerializeField]
        [Tooltip("The physics material that the hand uses.")]
        private PhysicMaterial _material = null;

        [SerializeField]
        [Tooltip("Temporarily disables collision on bones that are too displaced.")]
        private bool uncollideFarBones = true;

        [SerializeField]
        [Tooltip("If a segment displaces farther than this times its width, it will stop colliding if uncollideFarBones is set to true.")]
        private float uncollideThreshold = 2.0f;

        [SerializeField]
        [Tooltip("Adds powered joints in between all of the finger segments.")]
        private bool useConstraints = true;

        [SerializeField]
        [Tooltip("Sets each fingers' velocity directly every frame.")]
        private bool setFingerVelocity = true;

        [SerializeField]
        [Tooltip("Finds all static and kinematic colliders and puts them on the hand's layer so they do not collide with eachother.")]
        private bool noCollideWithStaticGeometry = true;


        // DELETE ME
        private int _hack = 0;

        void Start()
        {
            if (noCollideWithStaticGeometry) {
                Collider[] bodies = FindObjectsOfType<Collider>();
                foreach (Collider colliderr in bodies) {
                    if ((colliderr.attachedRigidbody == null || colliderr.attachedRigidbody.isKinematic) && !colliderr.isTrigger) {
                        colliderr.gameObject.layer = gameObject.layer;
                    }
                }
            }
        }

        public override Hand GetLeapHand() { return hand_; }
        public override void SetLeapHand(Hand hand) { hand_ = hand; }

        public override void BeginHand()
        {
            base.BeginHand();

#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;

            // We also require a material for friction to be able to work.
            if (_material == null || _material.bounciness != 0.0f || _material.bounceCombine != PhysicMaterialCombine.Minimum) {
                UnityEditor.EditorUtility.DisplayDialog("Collision Error!",
                                                        "An InteractionBrushHand must have a material with 0 bounciness "
                                                        + "and a bounceCombine of Minimum.  Name:" + gameObject.name,
                                                        "Ok");
                Debug.Break();
            }
#endif

            GameObject palmGameObject = new GameObject(gameObject.name + " Palm", typeof(Rigidbody), typeof(BoxCollider));
            palmGameObject.layer = gameObject.layer;

            Transform palmTransform = palmGameObject.GetComponent<Transform>();
            palmTransform.parent = transform;
            palmTransform.position = hand_.PalmPosition.ToVector3();
            palmTransform.rotation = hand_.Basis.CalculateRotation();
            if (palmTransform.parent != null) {
                palmTransform.localScale = new Vector3(1f / palmTransform.parent.lossyScale.x, 1f / palmTransform.parent.lossyScale.y, 1f / palmTransform.parent.lossyScale.z);
            }

            BoxCollider box = palmGameObject.GetComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.005f, 0.015f);
            box.size = new Vector3(0.06f, 0.02f, 0.07f);
            box.material = _material;

            palmBody = palmGameObject.GetComponent<Rigidbody>();
            palmBody.position = hand_.PalmPosition.ToVector3();
            palmBody.rotation = hand_.Basis.CalculateRotation();
            palmBody.freezeRotation = true;
            palmBody.useGravity = false;

            palmBody.mass = _perBoneMass*30f;
            palmBody.collisionDetectionMode = _collisionDetection;


            _capsuleBodies = new Rigidbody[N_FINGERS * N_ACTIVE_BONES];
            _lastPositions = new Vector3[N_FINGERS * N_ACTIVE_BONES];

            _ConfigurableJoints = new ConfigurableJoint[(N_FINGERS * N_ACTIVE_BONES)];
            _origPalmToJointRotation = new Quaternion[(N_FINGERS * N_ACTIVE_BONES)];

            for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
                for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
                    Bone bone = hand_.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1)); // +1 to skip first bone.

                    int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;

                    GameObject capsuleGameObject;
                    if (jointIndex < N_ACTIVE_BONES - 1 && useConstraints) {
                        capsuleGameObject = new GameObject(gameObject.name + " Finger " + boneArrayIndex, typeof(Rigidbody), typeof(CapsuleCollider), typeof(ConfigurableJoint));
                    } else {
                        capsuleGameObject = new GameObject(gameObject.name + " Finger " + boneArrayIndex, typeof(Rigidbody), typeof(CapsuleCollider));
                    }

                    //GameObject capsuleGameObject = new GameObject(gameObject.name, typeof(Rigidbody), typeof(CapsuleCollider));
                    capsuleGameObject.layer = gameObject.layer;
#if UNITY_EDITOR
                    // This is a debug facility that warns developers of issues.
                    capsuleGameObject.AddComponent<InteractionBrushBone>();
#endif

                    Transform capsuleTransform = capsuleGameObject.GetComponent<Transform>();
                    capsuleTransform.parent = transform;
                    capsuleTransform.position = bone.Center.ToVector3();
                    capsuleTransform.rotation = bone.Rotation.ToQuaternion();
                    if (capsuleTransform.parent != null) {
                        capsuleTransform.localScale = new Vector3(1f / capsuleTransform.parent.lossyScale.x, 1f / capsuleTransform.parent.lossyScale.y, 1f / capsuleTransform.parent.lossyScale.z);
                    }

                    CapsuleCollider capsule = capsuleGameObject.GetComponent<CapsuleCollider>();
                    capsule.direction = 2;
                    capsule.radius = bone.Width * 0.5f;
                    capsule.height = bone.Length + bone.Width;
                    capsule.material = _material;

                    Rigidbody body = capsuleGameObject.GetComponent<Rigidbody>();
                    _capsuleBodies[boneArrayIndex] = body;
                    body.position = bone.Center.ToVector3();
                    body.rotation = bone.Rotation.ToQuaternion();
                    body.useGravity = false;
                    if (!useConstraints||jointIndex==2) {
                        body.freezeRotation = true;
                    }

                    body.mass = _perBoneMass;
                    body.collisionDetectionMode = _collisionDetection;

                    _lastPositions[boneArrayIndex] = bone.Center.ToVector3();
                }
            }

            if (useConstraints) {
            //Add Joints to prevent fingers from coming apart
                for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
                    for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
                        int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
                        Bone bone = hand_.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1)); // +1 to skip first bone.

                        Rigidbody body = _capsuleBodies[boneArrayIndex].GetComponent<Rigidbody>();
                        if (jointIndex < N_ACTIVE_BONES - 1) {
                            Bone nextBone = hand_.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 2)); // +1 to skip first bone.
                            _ConfigurableJoints[boneArrayIndex+1] = _capsuleBodies[boneArrayIndex].GetComponent<ConfigurableJoint>();

                            _ConfigurableJoints[boneArrayIndex + 1].enablePreprocessing = true;
                            _ConfigurableJoints[boneArrayIndex + 1].autoConfigureConnectedAnchor = false;
                            _ConfigurableJoints[boneArrayIndex + 1].connectedBody = _capsuleBodies[boneArrayIndex + 1].gameObject.GetComponent<Rigidbody>();
                            Quaternion origJointRotation = nextBone.Rotation.ToQuaternion();
                            Quaternion origPalmRotation = _capsuleBodies[boneArrayIndex].rotation;
                            _origPalmToJointRotation[boneArrayIndex + 1] = Quaternion.Inverse(origPalmRotation) * origJointRotation;

                            _ConfigurableJoints[boneArrayIndex + 1].rotationDriveMode = RotationDriveMode.Slerp;
                            _ConfigurableJoints[boneArrayIndex + 1].anchor = body.transform.InverseTransformPoint(_capsuleBodies[boneArrayIndex + 1].transform.TransformPoint(new Vector3(0f, 0f, (_capsuleBodies[boneArrayIndex + 1].GetComponent<CapsuleCollider>().radius) - (_capsuleBodies[boneArrayIndex + 1].GetComponent<CapsuleCollider>().height / 2f))));
                            _ConfigurableJoints[boneArrayIndex + 1].connectedAnchor = new Vector3(0f, 0f, (_capsuleBodies[boneArrayIndex + 1].GetComponent<CapsuleCollider>().radius) - (_capsuleBodies[boneArrayIndex + 1].GetComponent<CapsuleCollider>().height / 2f));
                            _ConfigurableJoints[boneArrayIndex + 1].axis = body.transform.InverseTransformDirection(_capsuleBodies[boneArrayIndex + 1].transform.right);
                            _ConfigurableJoints[boneArrayIndex + 1].enableCollision = false;
                            _ConfigurableJoints[boneArrayIndex + 1].xMotion = ConfigurableJointMotion.Locked;
                            _ConfigurableJoints[boneArrayIndex + 1].yMotion = ConfigurableJointMotion.Locked;
                            _ConfigurableJoints[boneArrayIndex + 1].zMotion = ConfigurableJointMotion.Locked;

                            _ConfigurableJoints[boneArrayIndex + 1].hideFlags = HideFlags.DontSave | HideFlags.DontSaveInEditor;

                            JointDrive motorMovement = new JointDrive();
                            motorMovement.maximumForce = 50000f;
                            motorMovement.positionSpring = 50000f;

                            _ConfigurableJoints[boneArrayIndex + 1].slerpDrive = motorMovement;
                        }

                        if (jointIndex == 0) {
                            _ConfigurableJoints[boneArrayIndex] = palmBody.gameObject.AddComponent<ConfigurableJoint>();
                            _ConfigurableJoints[boneArrayIndex].configuredInWorldSpace = false;
                            _ConfigurableJoints[boneArrayIndex].connectedBody = _capsuleBodies[boneArrayIndex].GetComponent<Rigidbody>();
                            Quaternion origJointRotation = bone.Rotation.ToQuaternion();
                            Quaternion origPalmRotation = palmBody.rotation;
                            _origPalmToJointRotation[boneArrayIndex] = Quaternion.Inverse(origPalmRotation) * origJointRotation;

                            _ConfigurableJoints[boneArrayIndex].rotationDriveMode = RotationDriveMode.Slerp;

                            _ConfigurableJoints[boneArrayIndex].enablePreprocessing = true;
                            _ConfigurableJoints[boneArrayIndex].autoConfigureConnectedAnchor = false;
                            _ConfigurableJoints[boneArrayIndex].anchor = palmBody.transform.InverseTransformPoint(_capsuleBodies[boneArrayIndex].transform.TransformPoint(new Vector3(0f, 0f, (_capsuleBodies[boneArrayIndex].GetComponent<CapsuleCollider>().radius) - (_capsuleBodies[boneArrayIndex].GetComponent<CapsuleCollider>().height / 2f))));
                            _ConfigurableJoints[boneArrayIndex].connectedAnchor = new Vector3(0f, 0f, (_capsuleBodies[boneArrayIndex].GetComponent<CapsuleCollider>().radius) - (_capsuleBodies[boneArrayIndex].GetComponent<CapsuleCollider>().height / 2f));
                            _ConfigurableJoints[boneArrayIndex].enableCollision = false;

                            _ConfigurableJoints[boneArrayIndex].hideFlags = HideFlags.DontSave | HideFlags.DontSaveInEditor;

                            _ConfigurableJoints[boneArrayIndex].xMotion = ConfigurableJointMotion.Locked;
                            _ConfigurableJoints[boneArrayIndex].yMotion = ConfigurableJointMotion.Locked;
                            _ConfigurableJoints[boneArrayIndex].zMotion = ConfigurableJointMotion.Locked;

                            JointDrive motorMovement = new JointDrive();
                            motorMovement.maximumForce = 50000f;
                            motorMovement.positionSpring = 50000f;

                            _ConfigurableJoints[boneArrayIndex].slerpDrive = motorMovement;
                        }
                    }
                }
            }
        }

        public override void UpdateHand()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif

            // DELETE ME
            bool xx = false;
            if (++_hack > 100) {
                _hack = 0;
                xx = true;
            }

            Collider palmCollider = palmBody.GetComponent<BoxCollider>();
            Vector3 palmDelta = hand_.PalmPosition.ToVector3() - palmBody.position;

            float massOfHand = palmBody.mass + (N_FINGERS * N_ACTIVE_BONES * _perBoneMass);
            palmBody.velocity = (palmDelta / Time.fixedDeltaTime);
            if (palmBody.freezeRotation) {
                palmBody.MoveRotation(hand_.Basis.CalculateRotation());
            }

            if (!palmBody.useGravity) {
                if (uncollideFarBones) {
                    Vector3 error = _lastPalmPosition - palmBody.position;
                    if (error.magnitude > (palmBody.GetComponent<BoxCollider>().size.y * uncollideThreshold)) {
                        palmBody.useGravity = true;
                    }
                }
            } else {
                if (xx) {
                    //            bool isClear = Physics.CheckCapsule(bone.PrevJoint.ToVector3(), bone.NextJoint.ToVector3(), bone.Width * 0.5f, _layerMask);
                    //            if (isClear)
                    {
                        palmBody.useGravity = false;
                        palmBody.position = hand_.PalmPosition.ToVector3();
                        palmBody.rotation = hand_.Basis.CalculateRotation();
                        palmBody.velocity = Vector3.zero;
                    }
                }
            }
            _lastPalmPosition = hand_.PalmPosition.ToVector3();

            for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
                for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
                    Bone bone = hand_.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1));

                    int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
                    Rigidbody body = _capsuleBodies[boneArrayIndex];
                    Collider collider = body.GetComponent<CapsuleCollider>();


                    if (!body.useGravity) {
                        // Compare against intended target, not new tracking position.
                        if (uncollideFarBones) {
                            Vector3 error = _lastPositions[boneArrayIndex] - body.position;
                            if (error.magnitude > (bone.Width * uncollideThreshold) ||
                                (jointIndex > 0 && _capsuleBodies[boneArrayIndex - 1].useGravity)||
                                (jointIndex < N_ACTIVE_BONES-1 && _capsuleBodies[boneArrayIndex + 1].useGravity)||
                                palmBody.useGravity) {
                                body.useGravity = true;
                            }
                        }

                        if (setFingerVelocity) {
                            Vector3 delta = ((bone.Center.ToVector3() - body.position)*2f) / Time.fixedDeltaTime;
                            body.velocity = delta.magnitude > _maxVelocity ? (delta / delta.magnitude) * _maxVelocity : delta;
                        }

                    } else if (xx) {
                            //            bool isClear = Physics.CheckCapsule(bone.PrevJoint.ToVector3(), bone.NextJoint.ToVector3(), bone.Width * 0.5f, _layerMask);
                            //            if (isClear)
                            {
                                body.useGravity = false;
                                body.position = bone.Center.ToVector3();
                                body.rotation = bone.Rotation.ToQuaternion();
                                body.velocity = Vector3.zero;
                            }
                        }

                    if (setFingerVelocity && !body.useGravity) {
                        Vector3 delta = (bone.Center.ToVector3() - body.position) / Time.fixedDeltaTime;
                        body.velocity = delta.magnitude > _maxVelocity ? (delta / delta.magnitude) * _maxVelocity : delta;
                    }

                    if (body.freezeRotation) {
                        body.MoveRotation(bone.Rotation.ToQuaternion());
                    }

                    _lastPositions[boneArrayIndex] = bone.Center.ToVector3();
                }


            }

            //Drive the Joints for extra friction without setting the rotation
            if (useConstraints) {
                for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
                    for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
                        int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
                        Bone bone = hand_.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1));

                        Rigidbody body = _capsuleBodies[boneArrayIndex].GetComponent<Rigidbody>();

                        if (jointIndex == 0) {
                            Quaternion localRealFinger = (Quaternion.Euler(180f, 180f, 180f) * (Quaternion.Inverse(hand_.Basis.CalculateRotation() * _origPalmToJointRotation[boneArrayIndex]) * bone.Rotation.ToQuaternion()));

                            if (fingerIndex == 0 && hand_.IsRight) {
                                localRealFinger = Quaternion.Euler(localRealFinger.eulerAngles.y, localRealFinger.eulerAngles.x, localRealFinger.eulerAngles.z);
                            } else if (fingerIndex == 0 && hand_.IsLeft) {
                                localRealFinger = Quaternion.Euler(localRealFinger.eulerAngles.y * -1f, localRealFinger.eulerAngles.x * -1f, localRealFinger.eulerAngles.z);
                            } else {
                                localRealFinger = Quaternion.Euler(localRealFinger.eulerAngles.x * -1f, localRealFinger.eulerAngles.y, 0f);
                            }

                            //_ConfigurableJoints[boneArrayIndex].targetRotation = localRealFinger;

                        }
                        if (jointIndex < N_ACTIVE_BONES-1) {
                            Bone nextBone = hand_.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex+2));
                            Quaternion localRealFinger = (Quaternion.Euler(180f, 180f, 180f) * (Quaternion.Inverse(bone.Rotation.ToQuaternion() * _origPalmToJointRotation[boneArrayIndex + 1]) * nextBone.Rotation.ToQuaternion()));
                            localRealFinger = Quaternion.Euler(localRealFinger.eulerAngles.x, localRealFinger.eulerAngles.y * -1f, localRealFinger.eulerAngles.z);
                            //_ConfigurableJoints[boneArrayIndex+1].targetRotation = localRealFinger;
                        }

                    }
                }
            }
        }

        public override void FinishHand()
        {
            GameObject.Destroy(palmBody.gameObject);
            palmBody = null;
            for (int i = _capsuleBodies.Length; i-- != 0; ) {
                _capsuleBodies[i].transform.parent = null;
                GameObject.Destroy(_capsuleBodies[i].gameObject);
            }
            _capsuleBodies = null;

            base.FinishHand();
        }
    }
}