using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands
{
    public class PhysicsBone : MonoBehaviour
    {
        public PhysicsHand Hand => _hand;
        [SerializeField, HideInInspector]
        private PhysicsHand _hand;

        public ArticulationBody ArticulationBody
        {
            get
            {
                if (_body == null)
                {
                    _body = GetComponent<ArticulationBody>();
                }
                return _body;
            }
        }

        public HashSet<Rigidbody> ContactingObjects => _contactObjects;
        private HashSet<Rigidbody> _contactObjects = new HashSet<Rigidbody>();
        public HashSet<Rigidbody> GraspingObjects => _graspingObjects;
        private HashSet<Rigidbody> _graspingObjects = new HashSet<Rigidbody>();

        [SerializeField, HideInInspector]
        private ArticulationBody _body;

        private float _origXDriveLower = float.MinValue, _origXDriveUpper = float.MaxValue;
        public float OriginalXDriveLower => _origXDriveLower;
        public float OriginalXDriveUpper => _origXDriveUpper;

        public int Finger => _finger;
        [SerializeField, HideInInspector]
        private int _finger;

        public int Joint => _joint;
        [SerializeField, HideInInspector]
        private int _joint;

        /// <summary>
        /// Reports whether any object is near to the bone
        /// </summary>
        public bool IsObjectNearBone => _isObjectNearBone;
        /// <summary>
        /// Reports whether any of the currently grasped objects are near to the bone
        /// </summary>
        public bool IsGraspedObjectNearBone => _isGraspedObjectNearBone;

        private bool _isObjectNearBone = false, _isGraspedObjectNearBone = false;

        /// <summary>
        /// Will report float.MaxValue if IsObjectNearBone is false
        /// </summary>
        public float ObjectDistance => _objectDistance;
        /// <summary>
        /// Will report float.MaxValue if IsGraspedObjectNearBone is false
        /// </summary>
        public float GraspedObjectDistance => _graspedObjectDistance;
        private float _objectDistance = -1f, _graspedObjectDistance = -1f;

        /// <summary>
        /// This value will increase the further away from the palm the joint is.
        /// E.g. the finger tips will always be a significantly higher value than the knuckles
        /// </summary>
        public float DisplacementAmount { get; private set; } = 0;
        public float DisplacementDistance => _displacementDistance;
        public float DisplacementRotation => _displacementRotation;
        private float _displacementDistance = 0f, _displacementRotation = 0f;


        public Collider Collider => _collider;
        private Collider _collider;

        private RaycastHit[] _rayCache = new RaycastHit[6];
        private Ray _graspRay = new Ray();

        public bool IsContacting
        {
            get
            {
                return _contactObjects.Count > 0;
            }
        }

        public bool IsGrasping
        {
            get
            {
                return _graspingObjects.Count > 0;
            }
        }

        public void Awake()
        {
            _hand = GetComponentInParent<PhysicsHand>();
            _body = GetComponent<ArticulationBody>();
            _collider = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            ResetValues();
        }

        private void OnDisable()
        {
            ResetValues();
        }

        private void ResetValues()
        {
            _isObjectNearBone = false;
            _objectDistance = 0f;
            _graspedObjectDistance = 0f;
            DisplacementAmount = 0f;
            _displacementDistance = 0f;
            _displacementRotation = 0f;
        }

        public void SetBoneIndexes(int finger, int joint)
        {
            _finger = finger;
            _joint = joint;

            if (_body == null)
            {
                _body = GetComponent<ArticulationBody>();
            }
            if (_hand == null)
            {
                _hand = GetComponentInParent<PhysicsHand>();
            }
            if (_body != null)
            {
                _origXDriveLower = _body.xDrive.lowerLimit;
                _origXDriveUpper = _body.xDrive.upperLimit;
            }
        }

        public void AddContacting(Rigidbody rigid)
        {
            _contactObjects.Add(rigid);
        }

        public void RemoveContacting(Rigidbody rigid)
        {
            _contactObjects.Remove(rigid);
        }

        public void AddGrasping(Rigidbody rigid)
        {
            _graspingObjects.Add(rigid);
        }

        public void RemoveGrasping(Rigidbody rigid)
        {
            _graspingObjects.Remove(rigid);
        }

        internal void UpdateBoneDisplacement(Bone bone = null)
        {
            _displacementDistance = 0f;
            _displacementRotation = 0f;

            if (bone == null && Finger != 5)
            {
                return;
            }

            Vector3 bonePos, position;
            // Palm
            if (Finger == 5)
            {
                bonePos = _hand.GetOriginalLeapHand().PalmPosition;
                position = transform.position;
                _displacementRotation = Quaternion.Angle(transform.rotation, _hand.GetOriginalLeapHand().Rotation);
            }
            // Fingers
            else
            {
                bonePos = bone.NextJoint;
                CapsuleCollider capsule = (CapsuleCollider)_collider;
                capsule.ToWorldSpaceCapsule(out Vector3 tip, out Vector3 temp, out float rad);
                position = tip;

                if (_body.dofCount > 0)
                {
                    _displacementRotation = Mathf.Abs(_body.xDrive.target - _body.jointPosition[0] * Mathf.Rad2Deg);
                }
            }

            Debug.DrawLine(position, bonePos, Color.cyan, Time.fixedDeltaTime);

            _displacementDistance = Vector3.Distance(position, bonePos);

            // We want the rotation displacement to be more powerful than the distance
            DisplacementAmount = ((Mathf.InverseLerp(0.01f, _hand.Provider.HandTeleportDistance, _displacementDistance) * 0.75f) + (Mathf.InverseLerp(5f, 35f, _displacementRotation) * 1.25f)) * (1 + (Joint * 0.5f));
        }

        internal void UpdateBoneDistances()
        {
            _objectDistance = float.MaxValue;
            _graspedObjectDistance = float.MaxValue;
            _isObjectNearBone = false;
            _isGraspedObjectNearBone = false;

            float height, radius;

            if (Finger == 5)
            {
                BoxCollider palm = (BoxCollider)_collider;
                height = palm.size.z;
                radius = palm.size.y / 2f;
            }
            else
            {
                CapsuleCollider capsule = (CapsuleCollider)_collider;
                height = capsule.height;
                radius = capsule.radius;
            }

            // Move forward from center by 25% of the finger so that we're not off the very tip
            Vector3 position = _collider.bounds.center + transform.rotation * new Vector3(0, 0, height * .25f);

            // If we're on the thumb we want to tilt slightly towards the other fingers
            Vector3 upDirection = Finger == 0 ? Vector3.Lerp(-transform.up, _hand.Handedness == Chirality.Left ? -transform.right : transform.right, 0.3f) :
                Vector3.Lerp(-transform.up, _hand.Handedness == Chirality.Left ? transform.right : -transform.right, 0.1f);

            CalculateJointDistance(position, upDirection, radius, height * 2f, true);

            if (Joint == 2)
            {
                // Run a second test pointing forward a bit for the tip
                CalculateJointDistance(position + transform.rotation * new Vector3(0, radius * .2f, height * .15f), Vector3.Lerp(upDirection, transform.forward, 0.6f), radius, height * 2f);
                // Add a third one for pointing out from the tip to improve button detection
                CalculateJointDistance(position + transform.rotation * new Vector3(0, radius * .2f, height * .15f), transform.forward, radius, height * 0.5f);
            }

            if (Joint > 0)
            {
                // Second set of distance checks closer to the center of the bone
                CalculateJointDistance(_collider.bounds.center + transform.rotation * new Vector3(0, 0, -height * .15f), upDirection, radius, height * 2f);
            }

            if (Finger == 0)
            {
                // Thumb distances closer towards the object, pointing down a bit
                Vector3 direction = Vector3.Lerp(-transform.up, _hand.Handedness == Chirality.Left ? transform.right : -transform.right, 0.6f);
                position = _collider.bounds.center + transform.rotation * new Vector3(0, 0, height * .25f);
                CalculateJointDistance(position, direction, radius, height * 2f, debug: true);
            }
        }

        private void CalculateJointDistance(Vector3 origin, Vector3 direction, float radius, float length, bool forceUpdate = false, bool debug = false)
        {
            DistanceCalculation(origin, direction, radius, length, out bool anyFound, out float objectDistance, out bool graspFound, out float graspDistance);

            if (anyFound)
            {
                _isObjectNearBone = true;
                if (objectDistance < _objectDistance || forceUpdate)
                {
                    _objectDistance = objectDistance;
                }
                Debug.DrawLine(origin, origin + (direction * _objectDistance), Color.yellow, Time.fixedDeltaTime);
            }

            if (graspFound)
            {
                _isGraspedObjectNearBone = true;
                if (graspDistance < _graspedObjectDistance || forceUpdate)
                {
                    _graspedObjectDistance = graspDistance;
                }
            }
        }

        private void DistanceCalculation(Vector3 origin, Vector3 direction, float radius, float length, out bool anyFound, out float anyDistance, out bool graspFound, out float graspDistance)
        {
            anyDistance = 0f;
            graspDistance = 0f;

            _graspRay.origin = origin;
            _graspRay.direction = direction;

            // Using a sphere cast here will result in more reliable contact checks
            int cols = Physics.SphereCastNonAlloc(_graspRay, radius, _rayCache, length, _hand.Provider.InteractionMask, QueryTriggerInteraction.Ignore);

            anyFound = false;
            graspFound = false;

            for (int i = 0; i < cols; i++)
            {
                if (!anyFound)
                {
                    anyDistance = _rayCache[i].distance;
                    anyFound = true;
                }
                else if (_rayCache[i].distance < anyDistance)
                {
                    anyDistance = _rayCache[i].distance;
                }
                if (_graspingObjects.Contains(_rayCache[i].rigidbody))
                {
                    if (!graspFound)
                    {
                        graspDistance = _rayCache[i].distance;
                        graspFound = true;
                    }
                    else if (_rayCache[i].distance < graspDistance)
                    {
                        graspDistance = _rayCache[i].distance;
                    }
                }
            }
        }

    }
}