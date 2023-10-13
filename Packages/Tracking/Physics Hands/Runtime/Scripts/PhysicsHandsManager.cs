using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Attributes;

namespace Leap.Unity.PhysicsHands
{
    public class PhysicsHandsManager : LeapProvider
    {
        public enum ContactModes
        {
            HardContact,
            SoftContact,
            NoContact,
            Custom
        }

        [SerializeField] private LeapProvider _inputProvider;
        public LeapProvider InputProvider
        {
            get
            {
                if (_inputProvider == null)
                {
                    GetOrCreateBestInputProvider(out _inputProvider);
                }

                return _inputProvider;
            }
            set
            {
                _inputProvider = value;
            }
        }

        [Space, SerializeProperty("ContactMode"), SerializeField]
        private ContactModes _contactMode;
        public ContactModes ContactMode
        {
            get { return _contactMode; }
            set
            {
                SetContactMode(value);
            }
        }

        public ContactParent contactParent;

        #region Layers
        // Layers
        // Hand Layers
        public SingleLayer HandsLayer => _handsLayer;
        private SingleLayer _handsLayer = -1;

        public SingleLayer HandsResetLayer => _handsResetLayer;
        private SingleLayer _handsResetLayer = -1;

        private bool _layersGenerated = false;

        private LayerMask _interactionMask;
        public LayerMask InteractionMask => _interactionMask;
        #endregion

        #region Hand Settings
        [Space, SerializeField, Tooltip("The distance that bones will have their radius inflated by when calculating if an object is hovered.")]
        private float _hoverDistance = 0.04f;
        public float HoverDistance => _hoverDistance;
        [SerializeField, Tooltip("The distance that bones will have their radius inflated by when calculating if an object is grabbed. " +
                "If you increase this value too much, you may cause physics errors.")]
        private float _contactDistance = 0.002f;
        public float ContactDistance => _contactDistance;

        [Space, SerializeField, Tooltip("Allows the hands to collide with one another.")]
        private bool _interHandCollisions = false;
        public bool InterHandCollisions => _interHandCollisions;
        #endregion

        private WaitForFixedUpdate _postFixedUpdateWait = null;

        internal Leap.Hand _leftDataHand = new Hand(), _rightDataHand = new Hand();
        internal int _leftHandIndex = -1, _rightHandIndex = -1;

        private Frame _modifiedFrame = new Frame();

        public override Frame CurrentFrame => _modifiedFrame;

        public override Frame CurrentFixedFrame => _modifiedFrame;

        /// <summary>
        /// Happens in the execution order just before any hands are changed or updated
        /// </summary>
        public Action OnPrePhysicsUpdate;

        private void Awake()
        {
            GenerateLayers();
            SetupAutomaticCollisionLayers();
        }

        private void OnEnable()
        {
            if (InputProvider != null)
            {
                InputProvider.OnUpdateFrame -= ProcessFrame;
                InputProvider.OnUpdateFrame += ProcessFrame;
                InputProvider.OnFixedFrame -= ProcessFrame;
                InputProvider.OnFixedFrame += ProcessFrame;

                StartCoroutine(PostFixedUpdate());
            }
        }

        private void OnDisable()
        {
            if (_inputProvider != null)
            {
                _inputProvider.OnUpdateFrame -= ProcessFrame;
                _inputProvider.OnFixedFrame -= ProcessFrame;
            }
        }

        private void GetOrCreateBestInputProvider(out LeapProvider inputProvider)
        {
            inputProvider = Hands.Provider;
            if (inputProvider == null || inputProvider == this)
            {
                inputProvider = Hands.CreateXRLeapProviderManager();
                Debug.Log("Physics Hands Manager: No suitable Input Provider set. A LeapXRServiceProvider has been generated for you.");
            }
        }

        private void ProcessFrame(Frame inputFrame)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (Time.inFixedTimeStep)
            {
                OnPrePhysicsUpdate?.Invoke();
            }

            _modifiedFrame.CopyFrom(inputFrame);

            _leftHandIndex = inputFrame.Hands.FindIndex(x => x.IsLeft);
            if (_leftHandIndex != -1)
            {
                _leftDataHand.CopyFrom(inputFrame.Hands[_leftHandIndex]);
            }

            _rightHandIndex = inputFrame.Hands.FindIndex(x => x.IsRight);
            if (_rightHandIndex != -1)
            {
                _rightDataHand.CopyFrom(inputFrame.Hands[_rightHandIndex]);
            }

            contactParent?.UpdateFrame();
        
            // Output the frame on each update
            if (Time.inFixedTimeStep)
            {
                // Fixed frame on fixed update
                contactParent?.OutputFrame(ref _modifiedFrame);
                DispatchFixedFrameEvent(_modifiedFrame);
            }
            else
            {
                // Update frame otherwise
                contactParent?.OutputFrame(ref _modifiedFrame);
                DispatchUpdateFrameEvent(_modifiedFrame);
            }
        }

        private IEnumerator PostFixedUpdate()
        {
            if (_postFixedUpdateWait == null)
            {
                _postFixedUpdateWait = new WaitForFixedUpdate();
            }
            // Need to wait one frame to prevent early execution
            yield return null;
            for (; ; )
            {
                contactParent?.PostFixedUpdateFrame();
                yield return _postFixedUpdateWait;
            }
        }

        public void SetContactMode(ContactModes mode)
        {
            if (contactParent != null) // delete old contact hands
            {
                if (Application.isPlaying)
                {
                    Destroy(contactParent.gameObject);
                }
                else
                {
                    DestroyImmediate(contactParent.gameObject);
                }

                contactParent = null;
            }

            _contactMode = mode;

            if (_contactMode == ContactModes.Custom) // don't make new ones if we are now custom
            {
                return;
            }

            // Make new hands hand add their component
            GameObject newContactParent = new GameObject(_contactMode.ToString());

            switch (_contactMode)
            {
                case ContactModes.HardContact:
                    contactParent = newContactParent.AddComponent(typeof(HardContactParent)) as ContactParent;
                    break;
                case ContactModes.SoftContact:
                    contactParent = newContactParent.AddComponent(typeof(SoftContactParent)) as ContactParent;
                    break;
                case ContactModes.NoContact:
                    contactParent = newContactParent.AddComponent(typeof(NoContactParent)) as ContactParent;
                    break;
            }

            if (transform != null) // catches some edit-time issues
            {
                newContactParent.transform.parent = transform;
            }
        }

        #region Layer Generation
        protected void GenerateLayers()
        {
            if (_layersGenerated)
            {
                return;
            }

            _handsLayer = LayerMask.NameToLayer("ContactHands");
            _handsResetLayer = LayerMask.NameToLayer("ContactHandsReset");

            for (int i = 8; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);

                if (string.IsNullOrEmpty(layerName))
                {
                    if (_handsLayer == -1)
                    {
                        _handsLayer = i;
                        continue;
                    }
                    else if (_handsResetLayer == -1)
                    {
                        _handsResetLayer = i;
                        break;
                    }
                }
            }

            if (HandsLayer == -1 || HandsResetLayer == -1)
            {
                if (Application.isPlaying)
                {
                    enabled = false;
                }
                Debug.LogError("Could not find enough free layers for "
                              + "auto-setup; manual setup is required.", this.gameObject);
                return;
            }

            List<SingleLayer> _interactableLayers = new List<SingleLayer>() { 0 };

            if (_interHandCollisions)
            {
                _interactableLayers.Add(_handsLayer);
            }

            _interactionMask = new LayerMask();
            for (int i = 0; i < _interactableLayers.Count; i++)
            {
                _interactionMask = _interactionMask | _interactableLayers[i].layerMask;
            }

            _layersGenerated = true;
        }

        private void SetupAutomaticCollisionLayers()
        {
            for (int i = 0; i < 32; i++)
            {
                // Hands ignore all contact
                Physics.IgnoreLayerCollision(_handsResetLayer, i, true);
            }

            // Setup interhand collisions
            Physics.IgnoreLayerCollision(_handsLayer, _handsLayer, !_interHandCollisions);
        }

        #endregion

        #region Unity Editor

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (contactParent == null)
            {
                contactParent = GetComponentInChildren<ContactParent>();
            }
        }
#endif

        #endregion
    }
}