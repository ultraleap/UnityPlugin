using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class ContactManager : LeapProvider
    {

        [SerializeField] private LeapProvider _inputProvider;
        public LeapProvider InputProvider => _inputProvider;

        [HideInInspector]
        public ContactParent contactHands;
        [HideInInspector]
        public string contactParentName;
        [HideInInspector]
        public int contactParentChoiceIndex = 0;

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
        [SerializeField, Tooltip("The distance that bones will have their radius inflated by when calculating if an object is hovered.")]
        private float _hoverDistance = 0.04f;
        public float HoverDistance => _hoverDistance;
        [SerializeField, Tooltip("The distance that bones will have their radius inflated by when calculating if an object is grabbed. " +
                "If you increase this value too much, you may cause physics errors.")]
        private float _contactDistance = 0.002f;
        public float ContactDistance => _contactDistance;

        [SerializeField, Tooltip("Allows the hands to collide with one another.")]
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
            contactHands = GetComponentInChildren<ContactParent>();
            GenerateLayers();
            SetupAutomaticCollisionLayers();
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
        
            // Output the frame on each update
            if (Time.inFixedTimeStep)
            {
                // Fixed frame on fixed update
                contactHands?.OutputFrame(ref _modifiedFrame);
                DispatchFixedFrameEvent(_modifiedFrame);
            }
            else
            {
                // Update frame otherwise
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

            // Setup interhand collisions
            Physics.IgnoreLayerCollision(_handsLayer, _handsLayer, !_interHandCollisions);
        }

        #endregion

        #region Unity Editor

        private void OnValidate()
        {
            if (contactHands == null)
            {
                contactHands = GetComponentInChildren<ContactParent>();
            }
        }

        #endregion
    }

    [CustomEditor(typeof(ContactManager))]
    public class ContactManagerEditor : Editor
    {
        string contactParentPath;
        GameObject previousContactParentGO;
        string[] contactParentPaths;

        

        public override void OnInspectorGUI()
        {
            ContactManager thisAsContactManager = (ContactManager)target;

            #region contact parent dropdown

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Type Of Contact Hands"), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (thisAsContactManager.contactHands != null)
            {
                previousContactParentGO = thisAsContactManager.contactHands.gameObject;
            }
            contactParentPath = thisAsContactManager.contactParentName;

            List<string> contactParentNames = new List<string>();

            contactParentPaths = FindContactParentAssetPaths().ToArray();
            if (contactParentPaths.Length > 0)
            {
                for (int i = 0; i < contactParentPaths.Length; i++)
                {
                    contactParentNames.Add(Path.GetFileNameWithoutExtension(contactParentPaths[i]));
                }
                contactParentNames.Add("Custom");

                thisAsContactManager.contactParentChoiceIndex = EditorGUILayout.Popup(thisAsContactManager.contactParentChoiceIndex, contactParentNames.ToArray());

                if (contactParentNames[thisAsContactManager.contactParentChoiceIndex] != "Custom")
                {
                    if (contactParentPath != contactParentPaths[thisAsContactManager.contactParentChoiceIndex])
                    {
                        contactParentPath = contactParentPaths[thisAsContactManager.contactParentChoiceIndex];

                        if (previousContactParentGO != null)
                        {
                            DestroyImmediate(previousContactParentGO);
                        }
                        previousContactParentGO = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(contactParentPath));
                        previousContactParentGO.transform.parent = thisAsContactManager.transform;

                        ContactParent newContactParent = previousContactParentGO.GetComponent<ContactParent>();
                        newContactParent.contactManager = thisAsContactManager;

                        // Update the selected choice in ContactManager
                        thisAsContactManager.contactHands = newContactParent;
                        thisAsContactManager.contactParentName = contactParentPath;
                    }
                }
                else
                {
                    if (previousContactParentGO != null)
                    {
                        DestroyImmediate(previousContactParentGO);
                    }
                }
                EditorUtility.SetDirty(target);
            }
            EditorGUILayout.EndHorizontal();


            if(contactParentNames[thisAsContactManager.contactParentChoiceIndex] == "Custom")
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactHands"));
            }

            GUILayout.Space(10);
            #endregion

            DrawDefaultInspector();
        }


        private static List<string> FindContactParentAssetPaths()
        {
            List<ContactParent> assets = new List<ContactParent>();
            List<string> assetPaths = new List<string>();

            string[] guids = AssetDatabase.FindAssets(string.Format("t:prefab", typeof(ContactParent)));

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                ContactParent asset = AssetDatabase.LoadAssetAtPath<ContactParent>(assetPath);

                if (asset != null)
                {
                    assets.Add(asset);
                    assetPaths.Add(assetPath);
                }
            }
            return assetPaths;
        }

    }

}