/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity.Attachments
{

    /// <summary>
    /// Add an GameObject with this script to your scene if you would like to have a
    /// Transform hierarchy that will follow various important points on a hand, whether
    /// for visuals or for logic. The AttachmentHands object will maintain two child
    /// objects, one for each of the player's hands. Use the Inspector to customize
    /// which points you'd like to see in the hierarchy beneath the individual
    /// AttachmentHand objects.
    /// </summary>
    [ExecuteInEditMode]
    public class AttachmentHands : MonoBehaviour
    {
        [Tooltip("The Leap Provider the attachment hands use to generate the transform hierarchy." +
            " If left empty, AttachmentHands will attempt to find a LeapProvider in scene.")]
        [SerializeField]
        private LeapProvider _leapProvider;

        [SerializeField]
        private AttachmentPointFlags _attachmentPoints = AttachmentPointFlags.Palm | AttachmentPointFlags.Wrist;
        public AttachmentPointFlags attachmentPoints
        {
            get
            {
                return _attachmentPoints;
            }
            set
            {
                if (_attachmentPoints != value)
                {
#if UNITY_EDITOR
                    Undo.IncrementCurrentGroup();
                    Undo.SetCurrentGroupName("Modify Attachment Points");

                    Undo.RecordObject(this, "Modify AttachmentHands Points");
#endif

                    _attachmentPoints = value;
                    refreshAttachmentHandTransforms();
                }
            }
        }

        private Func<Hand>[] _handAccessors;
        /// <summary>
        /// Gets or sets the functions used to get the latest Leap.Hand data for the corresponding
        /// AttachmentHand objects in the attachmentHands array. Modify this if you'd like to customize
        /// how hand data is sent to AttachmentHands; e.g. a networked multiplayer game receiving
        /// serialized hand data for a networked player representation.
        /// 
        /// This array is automatically populated if it is null or empty during OnValidate() in the editor,
        /// but it can be modified freely afterwards.
        /// </summary>
        public Func<Hand>[] handAccessors { get { return _handAccessors; } set { _handAccessors = value; } }

        private AttachmentHand[] _attachmentHands;
        /// <summary>
        /// Gets or sets the array of AttachmentHand objects that this component manages. The length of this
        /// array should match the handAccessors array; corresponding-index slots in handAccessors will be
        /// used to assign transform data to the AttachmentHand objects in this component's Update().
        /// 
        /// This array is automatically populated if it is null or empty during OnValidate() in the editor,
        /// but can be modified freely afterwards.
        /// </summary>
        public AttachmentHand[] attachmentHands { get { return _attachmentHands; } set { _attachmentHands = value; } }

        /// <summary>
        /// Allows you to set a LeapProvider to generate transforms from for attachment hands.
        /// </summary>
        public void SetLeapProvider(LeapProvider provider)
        {
            _leapProvider = provider;
            reinitialize();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (getIsPrefab()) return;

            reinitialize();
        }
#endif

#if UNITY_EDITOR
        private void Reset()
        {
            attachmentPoints = AttachmentPointFlags.None; // delete all transforms before re-enabling the default attachment points (palm and wrist)
            refreshAttachmentHandTransforms();
            attachmentPoints = AttachmentPointFlags.Palm | AttachmentPointFlags.Wrist;


            handAccessors = null; // force it to find a leap provider & its leapHands again
            reinitialize();
        }
#endif

        void Awake()
        {
#if UNITY_EDITOR
            if (getIsPrefab()) return;
#endif

            reinitialize();
        }

        private void reinitialize()
        {
            refreshHandAccessors();
            refreshAttachmentHands();

#if UNITY_EDITOR
            EditorApplication.delayCall += refreshAttachmentHandTransforms;
#else
            refreshAttachmentHandTransforms();
#endif
        }

        void Update()
        {
#if UNITY_EDITOR
            if (Utils.IsObjectPartOfPrefabAsset(this.gameObject))
            {
                return;
            }
#endif
            if (_leapProvider == null)
            {
                return;
            }

            bool requiresReinitialization = false;

            for (int i = 0; i < _attachmentHands.Length; i++)
            {
                var attachmentHand = attachmentHands[i];

                if (attachmentHand == null || handAccessors == null)
                {
                    requiresReinitialization = true;
                    break;
                }

                var leapHand = handAccessors[i]();
                attachmentHand.isTracked = leapHand != null;

                foreach (var point in attachmentHand.points)
                {
                    point.SetTransformUsingHand(leapHand);
                }
            }

            if (requiresReinitialization)
            {
                reinitialize();
            }
        }

        private void refreshHandAccessors()
        {
            // If necessary, generate a left-hand and right-hand set of accessors.
            if (_handAccessors == null || _handAccessors.Length == 0)
            {
                _handAccessors = new Func<Hand>[2];

                if (_leapProvider == null)
                {
                    _leapProvider = FindObjectOfType<LeapProvider>();
                }
                if (_leapProvider != null)
                {
                    _handAccessors[0] = new Func<Hand>(() => _leapProvider.CurrentFrame?.GetHand(Chirality.Left));
                    _handAccessors[1] = new Func<Hand>(() => _leapProvider.CurrentFrame?.GetHand(Chirality.Right));
                }
                else
                {
                    Debug.LogWarning("AttachmentHands failed to find a LeapProvider! Is one present in your scene?");
                }

            }
        }

        private void refreshAttachmentHands()
        {
            // If we're a prefab, we'll be unable to set parent transforms, so we shouldn't create new objects in general.
            bool isPrefab = false;
#if UNITY_EDITOR
            isPrefab = getIsPrefab();
#endif

            // If necessary, generate a left and right AttachmentHand.
            if (_attachmentHands == null || _attachmentHands.Length == 0 || (_attachmentHands[0] == null || _attachmentHands[1] == null))
            {
                _attachmentHands = new AttachmentHand[2];

                // Try to use existing AttachmentHand objects first.
                foreach (Transform child in this.transform.GetChildren())
                {
                    var attachmentHand = child.GetComponent<AttachmentHand>();
                    if (attachmentHand != null)
                    {
                        _attachmentHands[attachmentHand.chirality == Chirality.Left ? 0 : 1] = attachmentHand;
                    }
                }

                // If we are a prefab and there are missing AttachmentHands, we have to return early.
                // We can't set parent transforms while a prefab. We're only OK if we already have attachmentHand
                // objects and their parents are properly set.
                if (isPrefab && (_attachmentHands[0] == null || _attachmentHands[0].transform.parent != this.transform
                              || _attachmentHands[1] == null || _attachmentHands[1].transform.parent != this.transform))
                {
                    return;
                }

                // Construct any missing AttachmentHand objects.
                if (_attachmentHands[0] == null)
                {
                    GameObject obj = new GameObject();
#if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(obj, "Created GameObject");
#endif
                    _attachmentHands[0] = obj.AddComponent<AttachmentHand>();
                    _attachmentHands[0].chirality = Chirality.Left;
                }
                _attachmentHands[0].gameObject.name = "Attachment Hand (Left)";
                if (_attachmentHands[0].transform.parent != this.transform) _attachmentHands[0].transform.parent = this.transform;

                if (_attachmentHands[1] == null)
                {
                    GameObject obj = new GameObject();
#if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(obj, "Created GameObject");
#endif
                    _attachmentHands[1] = obj.AddComponent<AttachmentHand>();
                    _attachmentHands[1].chirality = Chirality.Right;
                }
                _attachmentHands[1].gameObject.name = "Attachment Hand (Right)";
                if (_attachmentHands[1].transform.parent != this.transform) _attachmentHands[1].transform.parent = this.transform;

                // Organize left hand first in sibling order.
                _attachmentHands[0].transform.SetSiblingIndex(0);
                _attachmentHands[1].transform.SetSiblingIndex(1);
            }
        }

        private void refreshAttachmentHandTransforms()
        {
            if (this == null) return;

#if UNITY_EDITOR
            if (getIsPrefab()) return;
#endif

            bool requiresReinitialization = false;

            if (_attachmentHands == null)
            {
                requiresReinitialization = true;
            }
            else
            {
                foreach (var hand in _attachmentHands)
                {
                    if (hand == null)
                    {
                        // AttachmentHand must have been destroyed
                        requiresReinitialization = true;
                        break;
                    }

                    hand.refreshAttachmentTransforms(_attachmentPoints);
                }

#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }

            if (requiresReinitialization)
            {
                reinitialize();
            }
        }

#if UNITY_EDITOR
        private bool getIsPrefab()
        {
            return Utils.IsObjectPartOfPrefabAsset(this.gameObject);
        }
#endif

    }


}