using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands
{
    [RequireComponent(typeof(PhysicsIgnoreHelpers))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ConfigurableJoint))]
    [ExecuteInEditMode]
    public class PhysicsButtonElement : MonoBehaviour, IPhysicsHandHover, IPhysicsBoneContact
    {
        [SerializeField, HideInInspector]
        private PhysicsButton _button = null;

        [field: SerializeField]
        public PhysicsIgnoreHelpers IgnoreHelpers { get; private set; } = null;

        [field: SerializeField]
        public Rigidbody Rigid { get; private set; } = null;

        [field: SerializeField]
        public ConfigurableJoint Joint { get; private set; } = null;

        [SerializeField, HideInInspector]
        private PhysicMaterial _material;

        private Collider[] _colliders;

        private PhysicsProvider _provider;

        private HashSet<PhysicsHand> _hoveringHands = new HashSet<PhysicsHand>();
        public bool IsHovered { get; private set; } = false;
        public bool IsContacting { get; private set; } = false;

        private HashSet<PhysicsBone> _bones = new HashSet<PhysicsBone>();
        private HashSet<PhysicsBone> _tipBones = new HashSet<PhysicsBone>();
        public bool IsTipContacting { get; private set; } = false;

        private void Awake()
        {
            FindElements();
        }

        private void OnValidate()
        {
            FindElements();
        }

        private void OnEnable()
        {
            FindElements();
        }

        private void OnCollisionEnter(Collision collision)
        {
            // We want to ignore collisions with other objects if we're only wanting to interact with hands.
            if (!_button.HandsOnly)
                return;

            if (_provider == null)
            {
                _provider = FindObjectOfType<PhysicsProvider>(true);
            }
            if (_provider != null)
            {
                if (collision.collider.gameObject.layer != _provider.HandsLayer)
                {
                    foreach (var collider in _colliders)
                    {
                        Physics.IgnoreCollision(collision.collider, collider);
                    }
                }
            }
        }

        public void OnHandHover(PhysicsHand hand)
        {
            if (ValidateHand(hand))
            {
                _hoveringHands.Add(hand);
                IsHovered = _hoveringHands.Count > 0;
            }
        }

        public void OnHandHoverExit(PhysicsHand hand)
        {
            if (ValidateHand(hand))
            {
                _hoveringHands.Remove(hand);
                IsHovered = _hoveringHands.Count > 0;
            }
        }

        public void OnBoneContact(PhysicsBone contact)
        {
            if (ValidateBone(contact))
            {
                _tipBones.Add(contact);
                _bones.Add(contact);
                IsTipContacting = _tipBones.Count > 0;
                IsContacting = _bones.Count > 0;
            }
        }

        public void OnBoneContactExit(PhysicsBone contact)
        {
            if (ValidateBone(contact))
            {
                _tipBones.Remove(contact);
                _bones.Remove(contact);
                IsTipContacting = _tipBones.Count > 0;
                IsContacting = _bones.Count > 0;
            }
        }

        private bool ValidateHand(PhysicsHand hand)
        {
            if (!_button.AllowLeftHand && hand.Handedness == Chirality.Left)
            {
                return false;
            }
            if (!_button.AllowRightHand && hand.Handedness == Chirality.Right)
            {
                return false;
            }
            return true;
        }

        private bool ValidateBone(PhysicsBone bone)
        {
            if (!_button.AllowLeftHand && bone.Hand.Handedness == Chirality.Left)
            {
                return false;
            }
            if (!_button.AllowRightHand && bone.Hand.Handedness == Chirality.Right)
            {
                return false;
            }
            if (_button.FingerTipsOnly && bone.Joint != 2)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Get all the elements required, and ensure we're actually allowed to exist.
        /// </summary>
        private void FindElements()
        {
            _button = GetComponentInParent<PhysicsButton>();
            IgnoreHelpers = GetComponent<PhysicsIgnoreHelpers>();
            Rigid = GetComponent<Rigidbody>();
            Joint = GetComponent<ConfigurableJoint>();
            _colliders = GetComponentsInChildren<Collider>();

            _material = GeneratePhysicsMaterial();
            foreach (var collider in _colliders)
            {
                collider.material = _material;
            }

            // Stop trying to add this to a physics button.
            if (TryGetComponent(typeof(PhysicsButton), out var temp))
            {
                Debug.LogError("PhysicsButtonElements cannot be added to the same object as a PhysicsButton. Please create a child object for them.");
                if (!Application.isPlaying)
                {
                    DestroyImmediate(this);
                    DestroyImmediate(Rigid);
                    DestroyImmediate(IgnoreHelpers);
                    DestroyImmediate(Joint);
                }
                else
                {
                    Destroy(this);
                    Destroy(Rigid);
                    Destroy(IgnoreHelpers);
                    Destroy(Joint);
                }
            }
        }

        private static PhysicMaterial GeneratePhysicsMaterial()
        {
            PhysicMaterial physicMaterial = new PhysicMaterial("Button Material");
            physicMaterial.staticFriction = 1f;
            physicMaterial.dynamicFriction = 1f;
            physicMaterial.bounciness = 0f;
            physicMaterial.frictionCombine = PhysicMaterialCombine.Maximum;
            return physicMaterial;
        }
    }
}