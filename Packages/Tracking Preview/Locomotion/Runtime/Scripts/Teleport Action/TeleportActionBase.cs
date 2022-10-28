using Leap.Unity.Preview.HandRays;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
{
    public class TeleportActionBase : MonoBehaviour
    {
        public enum TeleportActionMovementType { FREE, FIXED };

        [Header("Teleport Action Setup")]
        [SerializeField] private TeleportAnchor freeTeleportAnchor;
        public Leap.Unity.Preview.HandRays.HandRayRenderer handRayRenderer = null;
        public HandRayInteractor handRayInteractor = null;
        public TeleportActionMovementType movementType = TeleportActionMovementType.FIXED;

        [SerializeField] private SingleLayer _teleportAnchorLayer;
        [SerializeField] private SingleLayer _teleportFloorLayer;
        private List<TeleportAnchor> _teleportAnchors = new List<TeleportAnchor>();

        /// <summary>
        /// Use this in child classes to test your logic through point.IsValid
        /// </summary>

        private TeleportAnchor _currentPointVal;
        protected TeleportAnchor _currentPoint { get { return _currentPointVal; } private set { _currentPointVal = value; } }
        protected Vector3 _currentPosition { get; private set; }
        protected Quaternion _currentRotation { get; private set; }

        protected Vector3 _deltaPosition { get; private set; }
        protected Vector3 _oldPosition { get; private set; }
        protected Quaternion _oldRotation { get; private set; }

        // Use this if you want to have your custom teleporter feed the rotation value
        protected bool _useCustomRotation = false;
        protected Quaternion _customRotation;

        private TeleportAnchor _lastHighlightedPoint;
        private TeleportAnchor _lastTeleportedPoint;

        protected bool _validTarget { get; private set; }
        protected bool _validPoint { get; private set; }

        public bool findTeleportAnchorsOnStart = true;

        public bool IsValid { get { return movementType == TeleportActionMovementType.FIXED ? _validTarget : _validPoint; } }

        private bool _isSelected;
        public bool IsSelected => _isSelected;
        public Action<TeleportActionMovementType> OnChangeMode;
        public Action<bool> OnTeleportSelected;

        /// <summary>
        /// This will return the position and rotation of the player.
        /// </summary>
        public Action<TeleportAnchor, Vector3, Quaternion> OnTeleport;

        public Transform Head;
        public GameObject Player;

        protected bool _currentTeleportAnchorLocked = false;

        public virtual void Start()
        {
            if (Head == null) Head = Camera.main.transform;
            if (Player == null) Player = Head.parent.gameObject == null ? Head.gameObject : Head.parent.gameObject;

            if (handRayInteractor != null)
            {
                handRayInteractor.OnRaycastUpdate += OnRayUpdate;
            }

            if (findTeleportAnchorsOnStart)
            {
                _teleportAnchors = new List<TeleportAnchor>(FindObjectsOfType<TeleportAnchor>());
            }
            SelectTeleport(false);
        }

        public void AddTeleportAnchor(TeleportAnchor teleportAnchor)
        {
            if (teleportAnchor == null) return;
            if (_teleportAnchors == null)
            {
                _teleportAnchors = new List<TeleportAnchor>();
            }
            _teleportAnchors.Add(teleportAnchor);
        }

        public void SetTeleportAnchors(List<TeleportAnchor> teleportAnchors)
        {
            _teleportAnchors = teleportAnchors;
        }

        public void ClearTeleportAnchors()
        {
            _teleportAnchors.Clear();
        }

        private void OnRayUpdate(RaycastHit[] results)
        {
            if (!IsSelected || _currentTeleportAnchorLocked)
            {
                return;
            }

            _lastHighlightedPoint = _currentPoint;
            _validTarget = false;
            _validPoint = false;
            _currentPoint = null;
            _oldPosition = _currentPosition;
            _oldRotation = _currentRotation;
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

            foreach(RaycastHit rayhit in results)
            {
                if (rayhit.collider != null)
                {
                    if (movementType == TeleportActionMovementType.FIXED)
                    {  
                        _validTarget = rayhit.collider.gameObject.layer == _teleportAnchorLayer && rayhit.collider.TryGetComponent(out _currentPointVal);
                    }
                    else
                    {
                        if (rayhit.collider.gameObject.layer == _teleportFloorLayer)
                        {
                            freeTeleportAnchor.gameObject.SetActive(true);
                            freeTeleportAnchor.transform.position = rayhit.point;
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
            }

            if (_oldPosition == Vector3.negativeInfinity && _currentPosition != Vector3.negativeInfinity)
            {
                _deltaPosition = Vector3.zero;
            }
            else
            {
                _deltaPosition = _currentPosition - _oldPosition;
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
        }

        public void SelectTeleport(bool selected = true)
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

        public void ChangeMovementType(TeleportActionMovementType newMovementType)
        {
            SelectTeleport(false);
            movementType = newMovementType;
            OnChangeMode?.Invoke(newMovementType);
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
            Player.transform.rotation = Quaternion.Euler(0, (newRotation * Quaternion.Inverse(Head.transform.rotation) * Player.transform.rotation).eulerAngles.y, 0);
        }

        private void MovePlayer(Vector3 newPosition)
        {
            Player.transform.position = newPosition + (new Vector3(Player.transform.position.x, 0, Player.transform.position.z) - new Vector3(Head.transform.position.x, 0, Head.transform.position.z));
        }

        private void OnValidate()
        {
            if (Head == null && Camera.main != null)
            {
                Head = Camera.main.transform;
            }
            if (Player == null && Head != null) Player = Head.parent.gameObject == null ? Head.gameObject : Head.parent.gameObject;
        }
    }
}
