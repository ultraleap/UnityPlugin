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
        [SerializeField, HideInInspector]
        private float _origXDriveLimit = float.MaxValue, _currentXDriveLimit = float.MaxValue;
        public float OriginalXDriveLimit => _origXDriveLimit;
        public float XDriveLimit => _currentXDriveLimit;

        public int Finger => _finger;
        [SerializeField, HideInInspector]
        private int _finger;

        public int Joint => _joint;
        [SerializeField, HideInInspector]
        private int _joint;

        public Collider Collider => _collider;
        private Collider _collider;

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
                _origXDriveLimit = _body.xDrive.upperLimit;
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
            if (_graspingObjects.Count == 0 && _body != null && _body.jointPosition.dofCount > 0)
            {
                _currentXDriveLimit = _body.jointPosition[0] * Mathf.Rad2Deg;
            }
            _graspingObjects.Add(rigid);
        }

        public void RemoveGrasping(Rigidbody rigid)
        {
            _graspingObjects.Remove(rigid);
        }

    }
}