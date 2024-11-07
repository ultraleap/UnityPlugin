/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.PhysicalHands
{
    public class PhysicalHandsManager : LeapProvider
    {
        public enum ContactMode
        {
            HardContact,
            SoftContact,
            NoContact
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
                if (_inputProvider != null)
                {
                    _inputProvider.OnUpdateFrame -= ProcessFrame;
                    _inputProvider.OnFixedFrame -= ProcessFrame;
                }

                _inputProvider = value;

                if (_inputProvider != null)
                {
                    _inputProvider.OnUpdateFrame += ProcessFrame;
                    _inputProvider.OnFixedFrame += ProcessFrame;
                    StartCoroutine(PostFixedUpdate());
                }
            }
        }

        [SerializeField]
        private ContactMode _contactMode;

        public ContactMode contactMode
        {
            get { return _contactMode; }
            set
            {
                SetContactMode(value);
            }
        }

        /// <summary>
        /// Set the enum for contact mode.
        /// 0 = Hard Contact
        /// 1 = Soft Contact
        /// 2 = No Contact
        /// </summary>
        /// <param name="contactMode">
        /// 0 = Hard Contact
        /// 1 = Soft Contact
        /// 2 = No Contact 
        /// </param>
        public void SetContactModeEnum(int contactModeInt)
        {
            contactMode = (ContactMode)contactModeInt;
        }


        private ContactParent _contactParent;
        public ContactParent ContactParent => _contactParent;

        #region Layers
        // Layers
        // Hand Layers
        public SingleLayer HandsLayer
        {
            get
            {
                if (_handsLayer == -1)
                {
                    _layersGenerated = false;
                    GenerateLayers();
                }
                return _handsLayer;
            }
        }
        private SingleLayer _handsLayer = -1;

        public SingleLayer HandsResetLayer
        {
            get
            {
                if (_handsResetLayer == -1)
                {
                    _layersGenerated = false;
                    GenerateLayers();
                }
                return _handsResetLayer;
            }
        }
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
        #endregion

        private WaitForFixedUpdate _postFixedUpdateWait = null;

        internal int _leftHandIndex = -1, _rightHandIndex = -1;

        private Frame _modifiedFrame = new Frame();

        public override Frame CurrentFrame
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying && _inputProvider != null)
                {
                    return _inputProvider.CurrentFrame;
                }
#endif
                return _modifiedFrame;
            }
        }

        public override Frame CurrentFixedFrame
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying && _inputProvider != null)
                {
                    return _inputProvider.CurrentFrame;
                }
#endif
                return _modifiedFrame;
            }
        }

        /// <summary>
        /// Happens in the execution order just before any hands are changed or updated
        /// </summary>
        public Action OnPrePhysicsUpdate;

        /// <summary>
        /// Called when the contact mode has been changed, but before the mode change has completed
        /// </summary>
        public Action OnContactModeChanged;

        #region Quick Accessors
        public ContactHand LeftHand { get { return ContactParent?.LeftHand; } }
        public ContactHand RightHand { get { return ContactParent?.RightHand; } }
        #endregion

        private void Awake()
        {
            if (ContactParent == null)
            {
                _contactParent = GetComponentInChildren<ContactParent>();
            }

            GenerateLayers();
            SetupAutomaticCollisionLayers();
        }

        private void OnEnable()
        {
            InputProvider = _inputProvider;
        }

        private void OnDisable()
        {
            if (_inputProvider != null)
            {
                _inputProvider.OnUpdateFrame -= ProcessFrame;
                _inputProvider.OnFixedFrame -= ProcessFrame;
            }
        }

        private void Reset()
        {
            foreach (var parent in GetComponentsInChildren<ContactParent>())
            {
                if (parent != null) // delete old contact hands
                {
                    if (Application.isPlaying)
                    {
                        Destroy(parent.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(parent.gameObject);
                    }

                    _contactParent = null;
                }
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

        internal void HandsInitiated()
        {
            OnHandsInitialized?.Invoke(ContactParent);
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

            _leftHandIndex = inputFrame.Hands.FindIndex(x => x.IsLeft);
            _rightHandIndex = inputFrame.Hands.FindIndex(x => x.IsRight);

            ContactParent?.UpdateFrame(inputFrame);
            _modifiedFrame = _modifiedFrame.CopyFrom(inputFrame);
            ContactParent?.OutputFrame(ref _modifiedFrame);

            // Output the frame on each update
            if (Time.inFixedTimeStep)
            {
                // Fixed frame on fixed update
                DispatchFixedFrameEvent(_modifiedFrame);
            }
            else
            {
                // Update frame otherwise
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
                ContactParent?.PostFixedUpdateFrame();
                yield return _postFixedUpdateWait;
            }
        }

        /// <summary>
        /// Sets the contact mode of the PhysicalHandsManager. This will destroy the previous contact parent and clear all values associated with it then create a new contact parent.
        /// </summary>
        /// <param name="mode">Which contact mode should we switch to?</param>
        public void SetContactMode(ContactMode mode)
        {
            _contactMode = mode;

            if (ContactParent != null) // delete old contact hands
            {
                if (Application.isPlaying)
                {
                    Destroy(ContactParent.gameObject);
                }
                else
                {
                    DestroyImmediate(ContactParent.gameObject);
                }

                _contactParent = null;
            }

            // Make new hands hand add their component
            GameObject newContactParent = new GameObject(_contactMode.ToString());

            switch (_contactMode)
            {
                case ContactMode.HardContact:
                    _contactParent = newContactParent.AddComponent(typeof(HardContactParent)) as ContactParent;
                    break;
                case ContactMode.SoftContact:
                    _contactParent = newContactParent.AddComponent(typeof(SoftContactParent)) as ContactParent;
                    break;
                case ContactMode.NoContact:
                    _contactParent = newContactParent.AddComponent(typeof(NoContactParent)) as ContactParent;
                    break;
            }


            if (transform != null) // catches some edit-time issues
            {
                newContactParent.transform.parent = transform;
            }

            _contactParent.Initialize();
            OnContactModeChanged?.Invoke();
        }

        #region Layer Generation
        protected void GenerateLayers()
        {
            if (_layersGenerated)
            {
                return;
            }

            _handsLayer = LayerMask.NameToLayer("PhysicalHands");
            _handsResetLayer = LayerMask.NameToLayer("PhysicalHandsReset");

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

            List<SingleLayer> _interactableLayers = new List<SingleLayer>() { };

            for (int i = 0; i < 32; i++)
            {
                if (i == HandsLayer || i == HandsResetLayer)
                {
                    continue;
                }

                if (!Physics.GetIgnoreLayerCollision(_handsLayer.layerIndex, i))
                {
                    _interactableLayers.Add(i);
                }
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
            Physics.IgnoreLayerCollision(_handsLayer, _handsLayer, true);
        }

        #endregion

        private void OnValidate()
        {
            if (ContactParent == null)
            {
                _contactParent = GetComponentInChildren<ContactParent>();
            }
        }

        #region Events

        [Space, Header("Hover Events"), Space]
        public UnityEvent<ContactHand, Rigidbody> onHover;
        public UnityEvent<ContactHand, Rigidbody> onHoverExit;

        [Space, Header("Contact Events"), Space]
        public UnityEvent<ContactHand, Rigidbody> onContact;
        public UnityEvent<ContactHand, Rigidbody> onContactExit;

        [Space, Header("Grab Events"), Space]
        public UnityEvent<ContactHand, Rigidbody> onGrab;
        public UnityEvent<ContactHand, Rigidbody> onGrabExit;

        internal static Action<ContactParent> OnHandsInitialized;

        internal void OnHandHover(ContactHand contacthand, Rigidbody rbody)
        {
            onHover?.Invoke(contacthand, rbody);
        }

        internal void OnHandHoverExit(ContactHand contacthand, Rigidbody rbody)
        {
            onHoverExit?.Invoke(contacthand, rbody);
        }

        internal void OnHandContact(ContactHand contacthand, Rigidbody rbody)
        {
            onContact?.Invoke(contacthand, rbody);
        }

        internal void OnHandContactExit(ContactHand contacthand, Rigidbody rbody)
        {
            onContactExit?.Invoke(contacthand, rbody);
        }

        internal void OnHandGrab(ContactHand contacthand, Rigidbody rbody)
        {
            onGrab?.Invoke(contacthand, rbody);
        }

        internal void OnHandGrabExit(ContactHand contacthand, Rigidbody rbody)
        {
            onGrabExit?.Invoke(contacthand, rbody);
        }

        #endregion
    }
}