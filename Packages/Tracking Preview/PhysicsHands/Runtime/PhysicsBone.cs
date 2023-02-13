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
        private float _origYDriveLower = float.MinValue, _origYDriveUpper = float.MaxValue;
        public float OriginalXDriveLower => _origXDriveLower;
        public float OriginalXDriveUpper => _origXDriveUpper;
        public float OriginalYDriveLower => _origYDriveLower;
        public float OriginalYDriveUpper => _origYDriveUpper;

        public int Finger => _finger;
        [SerializeField, HideInInspector]
        private int _finger;

        public int Joint => _joint;
        [SerializeField, HideInInspector]
        private int _joint;

        public bool IsObjectNearBone => _isObjectNearBone;
        public bool IsGraspedObjectNearBone => _isGraspedObjectNearBone;

        private bool _isObjectNearBone = false, _isGraspedObjectNearBone = false;

        public float ObjectDistance => _objectDistance;
        public float GraspedObjectDistance => _graspedObjectDistance;
        private float _objectDistance = -1f, _graspedObjectDistance = -1f;

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
                _origYDriveLower = _body.yDrive.lowerLimit;
                _origYDriveUpper = _body.yDrive.upperLimit;
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

        public void UpdateBoneDistances()
        {
            _objectDistance = -1f;
            _graspedObjectDistance = -1f;
            _isObjectNearBone = false;

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
            Vector3 upDirection = Finger == 0 ? Vector3.Lerp(-transform.up, _hand.Handedness == Chirality.Left ? -transform.right : transform.right, 0.2f) : -transform.up;

            position += upDirection * radius;

            DistanceCalculation(position, upDirection, radius * 0.5f, height * 2f, out _isObjectNearBone, out _objectDistance, out _isGraspedObjectNearBone, out _graspedObjectDistance);

            if (_isObjectNearBone)
            {
                Debug.DrawLine(position, position + (upDirection * _objectDistance), Color.yellow, Time.fixedDeltaTime);
            }

            // Run a second test pointing forward a bit for the tip
            if (Joint == 2)
            {
                DistanceCalculation(position, Vector3.Lerp(upDirection, transform.forward, 0.6f), radius * 0.5f, height * 2f, out bool anyFound, out float anyDistance, out bool graspFound, out float graspDistance);

                if (anyFound)
                {
                    _isObjectNearBone = true;
                    if (anyDistance < _objectDistance)
                    {
                        _objectDistance = anyDistance;
                    }
                    Debug.DrawLine(position, position + (Vector3.Lerp(upDirection, transform.forward, 0.6f) * _objectDistance), Color.yellow, Time.fixedDeltaTime);
                }
                
                if (graspFound)
                {
                    _isGraspedObjectNearBone = true;
                    if (graspDistance < _graspedObjectDistance)
                    {
                        _graspedObjectDistance = graspDistance;
                    }
                }
            }

            position = _collider.bounds.center + transform.rotation * new Vector3(0, 0, -height * .15f) + upDirection * radius;

            if (Joint > 0)
            {
                // Second set of distance checks closer to the center of the bone
                DistanceCalculation(position, upDirection, radius * 0.5f, height * 2f, out bool anyFound, out float objectDistance, out bool graspFound, out float graspDistance);

                if (anyFound)
                {
                    _isObjectNearBone = true;
                    if (objectDistance < _objectDistance)
                    {
                        _objectDistance = objectDistance;
                    }
                    Debug.DrawLine(position, position + (upDirection * _objectDistance), Color.yellow, Time.fixedDeltaTime);
                }

                if (graspFound)
                {
                    _isGraspedObjectNearBone = true;
                    if (graspDistance < _graspedObjectDistance)
                    {
                        _graspedObjectDistance = graspDistance;
                    }
                }
            }
        }

        private void DistanceCalculation(Vector3 origin, Vector3 direction, float radius, float length, out bool anyFound, out float anyDistance, out bool graspFound, out float graspDistance)
        {
            anyDistance = 0f;
            graspDistance = 0f;

            _graspRay.origin = origin;
            _graspRay.direction = direction;

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