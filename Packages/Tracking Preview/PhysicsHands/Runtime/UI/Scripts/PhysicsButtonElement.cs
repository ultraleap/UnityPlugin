using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands
{
    [RequireComponent(typeof(PhysicsIgnoreHelpers))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ConfigurableJoint))]
    [ExecuteInEditMode]
    public class PhysicsButtonElement : MonoBehaviour
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

        private void Awake()
        {
            FindElements();
        }

        private void OnValidate()
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
                _provider = _button.Provider;
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