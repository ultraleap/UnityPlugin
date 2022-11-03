using Leap.Unity.Preview.HandRays;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
{
    public class JumpGemTeleport : TeleportActionBase
    {
        private List<JumpGem> _jumpGems = new List<JumpGem>();
        private JumpGem _pinchedGem;
        public JumpGem PinchedGem => _pinchedGem;

        [SerializeField, Range(0.0001f,1f), Tooltip("The distance the user has to pinch a gem before the ray will be activated.")]
        private float _minimumDistanceToActivate = 0.1f;
        public float DistanceToActivate => _minimumDistanceToActivate;

        [Tooltip("When using free locomotion, this should be a pre-assigned object. When using fixed, it will automatically obtain it from the elements in the scene.")]
        public Transform targetLocation = null;

        private Transform _head;

        public Action<bool> OnJewelPinched;

        private void Awake()
        {
            _head = Camera.main.transform;
            _jumpGems = FindObjectsOfType<JumpGem>(true).ToList();
            for (int i = 0; i < _jumpGems.Count; i++)
            {
                int j = i;
                _jumpGems[i].OnPinch += (val) => { OnJewelPinch(val, j); };
                _jumpGems[i].OnRelease += OnJewelRelease;
            }
        }

        private void Update()
        {
            ChangeVisibleJewels();
            ProcessPinchedJewel();
            if (movementType == TeleportActionMovementType.FIXED)
            {
                _useCustomRotation = false;
                targetLocation.gameObject.SetActive(false);
            }
            else
            {
                _useCustomRotation = true;
                _customRotation = _head.rotation;
                targetLocation.gameObject.SetActive(IsSelected && IsValid);
                if (_pinchedGem != null && IsSelected && IsValid)
                {
                    try
                    {
                        targetLocation.position = _currentPosition;
                    }
                    catch { }
                }
            }
        }

        private void ChangeVisibleJewels()
        {
            bool anyPinched = _jumpGems.Any(x => x.IsPinched);
            _pinchedGem = null;
            foreach (var jewel in _jumpGems)
            {
                if (anyPinched)
                {
                    if (jewel.IsPinched)
                    {
                        _pinchedGem = jewel;
                        jewel.ChangeHidden(false);
                    }
                    else
                    {
                        jewel.ChangeHidden(true);
                    }
                }
                else
                {
                    jewel.ChangeHidden(false);
                }
            }
        }

        private void ProcessPinchedJewel()
        {
            if (_pinchedGem != null)
            {
                if (_pinchedGem.DistanceFromPoint() > _minimumDistanceToActivate && _pinchedGem.HasPinchedEnough && !IsSelected)
                {
                    SelectTeleport(true);
                    _pinchedGem = null;
                }
                else if (_pinchedGem.DistanceFromPoint() < _minimumDistanceToActivate && IsSelected)
                {
                    SelectTeleport(false);
                    _pinchedGem = null;
                }
            }
        }

        private void OnJewelPinch(Chirality hand, int index)
        {
            handRayInteractor.handRay.chirality = hand;

            if (handRayInteractor.handRay is TransformWristShoulderHandRay && _jumpGems[index].PinchItem != null)
            {
                ((TransformWristShoulderHandRay)handRayInteractor.handRay).transformToFollow = _jumpGems[index].PinchItem;
            }
            OnJewelPinched?.Invoke(true);
        }

        private void OnJewelRelease(bool enough)
        {
            if (handRayInteractor.handRay is TransformWristShoulderHandRay)
            {
                ((TransformWristShoulderHandRay)handRayInteractor.handRay).transformToFollow = null;
            }

            if (IsValid && IsSelected && enough)
            {
                ActivateTeleport(false);
            }
            else
            {
                SelectTeleport(false);
            }
            OnJewelPinched?.Invoke(false);
        }
    }
}