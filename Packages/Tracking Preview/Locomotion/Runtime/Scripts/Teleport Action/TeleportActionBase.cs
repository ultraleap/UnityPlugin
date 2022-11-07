/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
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

        [Header("Teleport Action Base Setup")]
        public FarFieldLayerManager farFieldLayerManager;
        public HandRayRenderer handRayRenderer = null;
        public HandRayInteractor handRayInteractor = null;
        public TeleportAnchor freeTeleportAnchor;
        public Transform Head;
        public GameObject Player;

        [Tooltip("If set to true, the teleport action base will find all teleport anchors on start")]
        public bool findTeleportAnchorsOnStart = true;

        [Header("Teleport Action Base Interaction Setup")]

        [Tooltip("If set to FIXED, you can only teleport to anchors in the world. If set to FREE, you can teleport anywhere you point.")]
        public TeleportActionMovementType movementType = TeleportActionMovementType.FIXED;

        [Tooltip("If true, when teleporting, the teleport anchor's forward direction will match your headset's world forward direction." +
            "\nIf false, your rotation will be the same way you are currently facing")]
        public bool useHeadsetForwardRotation = true;
        
        protected TeleportAnchor _currentPoint { get { return _currentPointVal; } private set { _currentPointVal = value; } }
        protected List<TeleportAnchor> _teleportAnchors = new List<TeleportAnchor>();
        
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

        private TeleportAnchor _lastHighlightedPoint;
        private TeleportAnchor _lastTeleportedPoint;
        private TeleportAnchor _currentPointVal;

        /// <summary>
        /// Returns if the current target is a valid place to teleport to
        /// </summary>
        public bool IsValid { get { return movementType == TeleportActionMovementType.FIXED ? _validTarget : _validPoint; } }
        protected bool _validTarget { get; private set; }
        protected bool _validPoint { get; private set; }

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
                _teleportAnchors = new List<TeleportAnchor>(FindObjectsOfType<TeleportAnchor>());
            }

            if(freeTeleportAnchor.GetComponent<MeshCollider>() != null)
            {
                Destroy(freeTeleportAnchor.GetComponent<MeshCollider>());
            }
            SelectTeleport(false);
        }

        private void OnValidate()
        {
            if (Head == null && Camera.main != null)
            {
                Head = Camera.main.transform;
            }
            if (Player == null && Head != null) Player = Head.parent.gameObject == null ? Head.gameObject : Head.parent.gameObject;

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
        public void ClearLastPoint()
        {
            _lastTeleportedPoint = null;
        }

        private void OnRayUpdate(RaycastHit[] results, RaycastHit primaryHit)
        {
            if (!IsSelected)
            {
                return;
            }

            if (_currentTeleportAnchorLocked)
            {
                UpdateCurrentPointRotation();
                return;
            }

            _lastHighlightedPoint = _currentPoint;
            _validTarget = false;
            _validPoint = false;
            _currentPoint = null;
            _currentPosition = Vector3.negativeInfinity;
            _currentRotation = Quaternion.identity;

            if(results == null || results.Length == 0)
            {
                if (_lastHighlightedPoint != null)
                {
                    _lastHighlightedPoint.SetHighlighted(false);
                }
                return;
            }

            if (primaryHit.collider != null)
            {
                if (movementType == TeleportActionMovementType.FIXED)
                {  
                    _validTarget = primaryHit.collider.gameObject.layer == farFieldLayerManager.FarFieldObjectLayer && primaryHit.collider.TryGetComponent(out _currentPointVal);
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
                        _currentPoint = freeTeleportAnchor;
                        _validPoint = true;
                    }
                    else
                    {
                        _currentPoint = null;
                        freeTeleportAnchor.gameObject.SetActive(false);
                    }
                }
                handRayRenderer.SetValid(IsValid);
            }

            if (_currentPoint != null)
            {
                _currentPoint.SetHighlighted(true);
                _currentPosition = _currentPoint.transform.position;
                _currentRotation = _currentPoint.transform.rotation;
            }

            if (_currentPoint != _lastHighlightedPoint && _lastHighlightedPoint != null)
            {
                _lastHighlightedPoint.SetHighlighted(false);
            }

            UpdateCurrentPointRotation();
        }

        private void UpdateCurrentPointRotation()
        {
            if (_isSelected && IsValid && _useCustomRotation)
            {
                _currentPoint.UpdateRotationVisuals(_useCustomRotation ? _customRotation : _currentRotation);
            }
        }

        protected void SelectTeleport(bool selected = true)
        {
            if(selected && !IsSelected)
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
                if (selected && _teleportAnchors[i] == _lastTeleportedPoint)
                    continue;

                _teleportAnchors[i].gameObject.SetActive(movementType == TeleportActionMovementType.FIXED && selected);
            }

            if (!selected)
            {
                if (_currentPoint != null)
                {
                    _currentPoint.SetHighlighted(false);
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
            if (_currentPoint != null)
            {
                _currentPosition = _currentPoint.transform.position;
                _currentRotation = _currentPoint.transform.rotation;
            }

            _lastTeleportedPoint = _lastHighlightedPoint;
            TeleportHere(_currentPosition, _useCustomRotation ? _customRotation : _currentRotation);
            SelectTeleport(keepActiveAfterTeleport);
        }

        protected virtual void TeleportHere(Vector3 newPosition, Quaternion newRotation)
        {
            RotatePlayer(newRotation);
            MovePlayer(newPosition);
            handRayInteractor?.handRay?.ResetRay();
            OnTeleport?.Invoke(_currentPoint, newPosition, newRotation);
        }

        private void RotatePlayer(Quaternion newRotation)
        {
            if (useHeadsetForwardRotation)
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
