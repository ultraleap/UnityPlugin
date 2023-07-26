/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Preview.HandRays;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
{
    /// <summary>
    /// Base class for all teleport actions.
    /// Provides functionality to validate the correct type of teleport anchor and teleport a player to the right place.
    /// A teleport action can be idle, selected or activated.
    /// When idle, it is not in use. When selected, the ray is active the user is selecting where they want to teleport.
    /// When activated, it will teleport you.
    /// </summary>
    public abstract class TeleportActionBase : MonoBehaviour
    {
        public enum TeleportActionMovementType { FREE, FIXED };

        [Header("Teleport Action Base - Setup")]
        public FarFieldLayerManager farFieldLayerManager;
        public HandRayRenderer handRayRenderer = null;
        public HandRayInteractor handRayInteractor = null;
        public TeleportAnchor freeTeleportAnchor;
        public Transform Head;
        public GameObject Player;

        [Tooltip("If set to true, the teleport action base will find all teleport anchors on start")]
        public bool findTeleportAnchorsOnStart = true;

        [Header("Teleport Action Base - Interaction Setup")]

        [Tooltip("If set to FIXED, you can only teleport to anchors in the world. If set to FREE, you can teleport anywhere you point.")]
        public TeleportActionMovementType movementType = TeleportActionMovementType.FIXED;
        private TeleportActionMovementType _movementTypeLastFrame;

        [Tooltip("If true, when teleporting, the teleport anchor's forward direction will match your headset's world forward direction." +
            "\nIf false, your rotation will be the same way you are currently facing")]
        public bool useHeadsetForwardRotationForFixed = true;

        protected TeleportAnchor _currentAnchor { get { return _currentAnchorVal; } private set { _currentAnchorVal = value; } }
        protected List<TeleportAnchor> _teleportAnchors = new List<TeleportAnchor>();
        public List<TeleportAnchor> TeleportAnchors => _teleportAnchors;

        protected Vector3 _currentPosition { get; private set; }
        protected Quaternion _currentRotation { get; private set; }

        /// <summary>
        /// If true, your teleport action will use _customRotation, in place of the teleport anchor's rotation
        /// </summary>
        protected bool _useCustomRotation = false;
        protected Quaternion _customRotation;

        /// <summary>
        /// Set to true if you want to lock the current teleport anchor selection in places
        /// </summary>
        protected bool _currentTeleportAnchorLocked = false;

        private TeleportAnchor _lastHighlightedAnchor;
        public TeleportAnchor LastTeleportedAnchor => _lastTeleportedAnchor;
        private TeleportAnchor _lastTeleportedAnchor;
        private TeleportAnchor _currentAnchorVal;

        /// <summary>
        /// Returns if the current target is a valid place to teleport to
        /// </summary>
        public bool IsValid { get { return movementType == TeleportActionMovementType.FIXED ? _validTarget : _validAnchor; } }
        protected bool _validTarget { get; private set; }
        protected bool _validAnchor { get; private set; }

        /// <summary>
        /// Returns true if the current teleport action is selected
        /// </summary>
        public bool IsSelected => _isSelected;
        private bool _isSelected;

        /// <summary>
        /// Called when the teleport action is selected, or unselected;
        /// This will pass whether it is selected or unselected.
        /// </summary>
        public Action<bool> OnTeleportSelected;

        /// <summary>
        /// Called when the teleport action is activated.
        /// This will return the teleport anchor, the position and the rotation of the player.
        /// </summary>
        public Action<TeleportAnchor, Vector3, Quaternion> OnTeleport;

        public Action<TeleportActionMovementType> OnMovementTypeChanged;

        public virtual void Start()
        {
            if (Head == null) Head = Camera.main.transform;
            if (Player == null) Player = Head.parent.gameObject == null ? Head.gameObject : Head.parent.gameObject;
            if (farFieldLayerManager == null)
            {
                farFieldLayerManager = FindObjectOfType<FarFieldLayerManager>();
            }

            if (handRayInteractor != null)
            {
                handRayInteractor.OnRaycastUpdate += OnRayUpdate;
            }

            if (findTeleportAnchorsOnStart)
            {
                _teleportAnchors = new List<TeleportAnchor>(FindObjectsOfType<TeleportAnchor>(true));
            }

            if (freeTeleportAnchor.GetComponent<MeshCollider>() != null)
            {
                Destroy(freeTeleportAnchor.GetComponent<MeshCollider>());
            }

            _movementTypeLastFrame = movementType;
            SelectTeleport(false);
        }

        protected virtual void Update()
        {
            if (_movementTypeLastFrame != movementType)
            {
                OnMovementTypeChanged?.Invoke(movementType);
                _movementTypeLastFrame = movementType;
            }
        }

        private void OnValidate()
        {
            if (Head == null && Camera.main != null)
            {
                Head = Camera.main.transform;
            }
            if (Player == null && Head != null)
            {
                Player = Head.parent?.gameObject == null ? Head.gameObject : Head.parent.gameObject;
            }

            if (farFieldLayerManager == null)
            {
                farFieldLayerManager = FindObjectOfType<FarFieldLayerManager>();
            }
        }

        /// <summary>
        /// Adds a teleport anchor to the list of teleport anchors
        /// </summary>
        /// <param name="teleportAnchor">The teleport anchor to add</param>
        public void AddTeleportAnchor(TeleportAnchor teleportAnchor)
        {
            if (teleportAnchor == null) return;
            if (_teleportAnchors == null)
            {
                _teleportAnchors = new List<TeleportAnchor>();
            }
            _teleportAnchors.Add(teleportAnchor);
        }

        /// <summary>
        /// Sets the list of teleport anchors
        /// </summary>
        /// <param name="teleportAnchors">The new list of teleport anchors</param>
        public void SetTeleportAnchors(List<TeleportAnchor> teleportAnchors)
        {
            _teleportAnchors = teleportAnchors;
        }

        /// <summary>
        /// Clears the list of teleport anchors
        /// </summary>
        public void ClearTeleportAnchors()
        {
            _teleportAnchors.Clear();
        }

        /// <summary>
        /// This ensures that all teleport anchors will become visible on the next enable.
        /// This helps prevent situations where you may have moved the player without explicitly teleporting.
        /// </summary>
        public void ClearLastTeleportedAnchor()
        {
            _lastTeleportedAnchor = null;
        }

        /// <summary>
        /// Sets the last teleported point - this can be useful when switching teleport actions
        /// </summary>
        /// <param name="teleportAnchor"></param>
        public void SetLastTeleportedAnchor(TeleportAnchor teleportAnchor)
        {
            _lastTeleportedAnchor = teleportAnchor;
        }

        /// <summary>
        /// Removes a teleport anchor from the list of valid anchors to teleport to in fixed mode.
        /// </summary>
        /// <param name="teleportAnchor"></param>
        public void RemoveTeleportAnchorFromFixedAnchors(TeleportAnchor teleportAnchor)
        {
            _teleportAnchors.Remove(teleportAnchor);
        }

        /// <summary>
        /// Can be used by a class which isn't the teleport action to activate a teleport
        /// </summary>
        /// <param name="teleportAnchor"></param>
        public void TeleportToAnchor(TeleportAnchor teleportAnchor)
        {
            _currentAnchor = teleportAnchor;
            ActivateTeleport(teleportAnchor);
        }

        private void OnRayUpdate(RaycastHit[] results, RaycastHit primaryHit)
        {
            if (!IsSelected)
            {
                return;
            }

            if (_currentTeleportAnchorLocked)
            {
                UpdateCurrentAnchorRotation();
                return;
            }

            _lastHighlightedAnchor = _currentAnchor;
            _validTarget = false;
            _validAnchor = false;
            _currentAnchor = null;
            _currentPosition = Vector3.negativeInfinity;
            _currentRotation = Quaternion.identity;

            if (results == null || results.Length == 0)
            {
                if (_lastHighlightedAnchor != null)
                {
                    _lastHighlightedAnchor.SetHighlighted(false);
                }
                handRayRenderer.SetValid(IsValid);
                return;
            }

            if (primaryHit.collider != null)
            {
                if (movementType == TeleportActionMovementType.FIXED)
                {
                    _validTarget = primaryHit.collider.gameObject.layer == farFieldLayerManager.FarFieldObjectLayer && primaryHit.collider.TryGetComponent(out _currentAnchorVal);
                }
                else
                {
                    if (primaryHit.collider.gameObject.layer == farFieldLayerManager.FloorLayer)
                    {
                        freeTeleportAnchor.gameObject.SetActive(true);
                        freeTeleportAnchor.transform.position = primaryHit.point;
                        Vector3 rotation = Quaternion.LookRotation(handRayInteractor.handRay.handRayDirection.Direction, Vector3.up).eulerAngles;
                        rotation.x = 0;
                        freeTeleportAnchor.transform.rotation = Quaternion.Euler(rotation);
                        _currentAnchor = freeTeleportAnchor;
                        _validAnchor = true;
                    }
                    else
                    {
                        _currentAnchor = null;
                        freeTeleportAnchor.gameObject.SetActive(false);
                    }
                }
                handRayRenderer.SetValid(IsValid);
            }

            if (_currentAnchor != null)
            {
                _currentAnchor.SetHighlighted(true);
                _currentPosition = _currentAnchor.transform.position;
                _currentRotation = _currentAnchor.transform.rotation;
            }

            if (_currentAnchor != _lastHighlightedAnchor && _lastHighlightedAnchor != null)
            {
                _lastHighlightedAnchor.SetHighlighted(false);
            }

            UpdateCurrentAnchorRotation();
        }

        private void UpdateCurrentAnchorRotation()
        {
            if (_isSelected && IsValid && _useCustomRotation)
            {
                _currentAnchor.UpdateRotationVisuals(_useCustomRotation ? _customRotation : _currentRotation);
            }
        }

        protected void SelectTeleport(bool selected = true)
        {
            if (selected && !IsSelected)
            {
                OnTeleportSelected?.Invoke(true);
            }
            else if (!selected && IsSelected)
            {
                OnTeleportSelected?.Invoke(false);
            }


            _isSelected = selected;
            if (handRayRenderer != null)
            {
                handRayRenderer.SetActive(selected);
            }
            for (int i = 0; i < _teleportAnchors.Count; i++)
            {
                if (selected && _teleportAnchors[i] == _lastTeleportedAnchor)
                    continue;

                _teleportAnchors[i].gameObject.SetActive(movementType == TeleportActionMovementType.FIXED && selected);
            }

            if (!selected)
            {
                if (_currentAnchor != null)
                {
                    _currentAnchor.SetHighlighted(false);
                }

                _validTarget = false;

                _currentPosition = Vector3.negativeInfinity;
                _currentRotation = Quaternion.identity;
                freeTeleportAnchor.gameObject.SetActive(false);
            }
            else
            {
                freeTeleportAnchor.gameObject.SetActive(!(movementType == TeleportActionMovementType.FIXED));
            }
        }

        protected void ActivateTeleport(bool keepActiveAfterTeleport = false)
        {
            if (_currentAnchor != null)
            {
                _currentPosition = _currentAnchor.transform.position;
                _currentRotation = _currentAnchor.transform.rotation;
            }

            _lastTeleportedAnchor = _lastHighlightedAnchor;
            TeleportHere(_currentPosition, _useCustomRotation ? _customRotation : _currentRotation);
            SelectTeleport(keepActiveAfterTeleport);
        }

        protected virtual void TeleportHere(Vector3 newPosition, Quaternion newRotation)
        {
            RotatePlayer(newRotation);
            MovePlayer(newPosition);
            handRayInteractor?.handRay?.ResetRay();
            OnTeleport?.Invoke(_currentAnchor, newPosition, newRotation);
            _currentAnchor.OnTeleportedTo?.Invoke(_currentAnchor);
        }

        private void RotatePlayer(Quaternion newRotation)
        {
            if (movementType == TeleportActionMovementType.FIXED && useHeadsetForwardRotationForFixed)
            {
                Player.transform.rotation = Quaternion.Euler(0, newRotation.eulerAngles.y, 0);
            }
            else
            {
                Player.transform.rotation = Quaternion.Euler(0, (newRotation * Quaternion.Inverse(Head.transform.rotation) * Player.transform.rotation).eulerAngles.y, 0);
            }
        }

        private void MovePlayer(Vector3 newPosition)
        {
            Player.transform.position = newPosition + (new Vector3(Player.transform.position.x, 0, Player.transform.position.z) - new Vector3(Head.transform.position.x, 0, Head.transform.position.z));
        }
    }
}