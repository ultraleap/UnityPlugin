using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class ContactManager : LeapProvider
    {

        [SerializeField] private LeapProvider _inputProvider;
        public LeapProvider InputProvider => _inputProvider;

        public ContactParent contactHands;

        #region Layers
        // Layers
        // Object Layers
        public SingleLayer DefaultLayer => _defaultLayer;
        [SerializeField, Tooltip("This layer will be used as the base when automatically generating layers.")]
        private SingleLayer _defaultLayer = 0;

        public List<SingleLayer> InteractableLayers => _interactableLayers;
        [SerializeField, Tooltip("The layers that you want hands and helpers to make contact and interact with. If no layers are added then the default layer will be included.")]
        private List<SingleLayer> _interactableLayers = new List<SingleLayer>() { 0 };

        public List<SingleLayer> NoContactLayers => _noContactLayers;
        [SerializeField, Tooltip("Layers that should be ignored by the hands. " +
            "These layers should be used throughout your scene on objects that you want hands to freely pass through. We recommend creating these layers manually. " +
            "If no layers are added then a layer will be created.")]
        private List<SingleLayer> _noContactLayers = new List<SingleLayer>();

        // Hand Layers
        public SingleLayer HandsLayer => _handsLayer;
        [SerializeField, Tooltip("The default layer for the hands.")]
        private SingleLayer _handsLayer = new SingleLayer();

        [SerializeField, Tooltip("The default layer for the hands. It is recommended to leave this as an automatically generated layer.")]
        private bool _automaticHandsLayer = true;

        public SingleLayer HandsResetLayer => _handsResetLayer;
        [SerializeField, Tooltip("The layer that the hands will be set to during non-active or reset states.")]
        private SingleLayer _handsResetLayer = new SingleLayer();
        [SerializeField, Tooltip("The layer that the hands will be set to during non-active or reset states. It is recommended to leave this as an automatically generated layer.")]
        private bool _automaticHandsResetLayer = true;

        private bool _layersGenerated = false;
        private LayerMask _interactionMask;
        public LayerMask InteractionMask => _interactionMask;
        #endregion

        #region Hand Settings
        private float _hoverDistance = 0.04f;
        public float HoverDistance => _hoverDistance;
        private float _contactDistance = 0.002f;
        public float ContactDistance => _contactDistance;
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
            contactHands = GetComponentInChildren<ContactParent>(true);
            GenerateLayers();
        }

        private void OnEnable()
        {
            if (_inputProvider != null)
            {
                _inputProvider.OnUpdateFrame -= ProcessFrame;
                _inputProvider.OnUpdateFrame += ProcessFrame;
                _inputProvider.OnFixedFrame -= ProcessFrame;
                _inputProvider.OnFixedFrame += ProcessFrame;

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

            contactHands?.UpdateFrame();

            // Output the frame on update
            if (!Time.inFixedTimeStep)
            {
                contactHands?.OutputFrame(ref _modifiedFrame);
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
                contactHands?.PostFixedUpdateFrame();
                // Output the frame after physics update is complete
                contactHands?.OutputFrame(ref _modifiedFrame);
                DispatchFixedFrameEvent(_modifiedFrame);
                yield return _postFixedUpdateWait;
            }
        }


        #region Layer Generation
        protected void GenerateLayers()
        {
            if (_layersGenerated)
            {
                return;
            }

            if (_automaticHandsLayer || HandsLayer == DefaultLayer)
            {
                _handsLayer = -1;
            }
            if (_automaticHandsResetLayer || HandsResetLayer == DefaultLayer)
            {
                _handsResetLayer = -1;
            }
            for (int i = 0; i < _noContactLayers.Count; i++)
            {
                if (_noContactLayers[i] == DefaultLayer)
                {
                    _noContactLayers.Remove(i);
                    i--;
                }
            }

            for (int i = 8; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(layerName))
                {
                    if (_noContactLayers.Count == 0)
                    {
                        _noContactLayers.Add(new SingleLayer() { layerIndex = i });
                        continue;
                    }
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

            if (_interactableLayers.Count == 0)
            {
                _interactableLayers.Add(new SingleLayer() { layerIndex = 0 });
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
                // Copy ignore settings from template layer
                bool shouldIgnore = Physics.GetIgnoreLayerCollision(DefaultLayer, i);
                Physics.IgnoreLayerCollision(_handsLayer, i, shouldIgnore);

                for (int j = 0; j < _noContactLayers.Count; j++)
                {
                    Physics.IgnoreLayerCollision(_noContactLayers[j], i, shouldIgnore);
                }

                // Hands ignore all contact
                Physics.IgnoreLayerCollision(_handsResetLayer, i, true);
            }

            for (int i = 0; i < _interactableLayers.Count; i++)
            {
                Physics.IgnoreLayerCollision(_interactableLayers[i], _handsLayer, false);
            }

            // Disable interaction between hands and nocontact objects
            for (int i = 0; i < _noContactLayers.Count; i++)
            {
                Physics.IgnoreLayerCollision(_noContactLayers[i], _handsLayer, true);
            }
        }

        #endregion

        #region Unity Editor

        private void OnValidate()
        {
            if (contactHands == null)
            {
                contactHands = GetComponentInChildren<ContactParent>(true);
            }
        }

        #endregion
    }

}