using Leap.Unity.Interaction;
using Leap.Unity.Interaction.PhysicsHands;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
    public abstract class HandRayInteractor : MonoBehaviour
    {
        [SerializeField] private HandRay _handRay;
        public HandRay handRay => _handRay;

        [Tooltip("Note that the Interaction Engine and Physics Hands layers will be ignored automatically.")]
        [Header("Layer Logic")]
        public LayerMask layerMask;
        private bool _rayFrozen = false;

        [Header("Events")]
        public Action<RaycastHit[]> OnRaycastUpdate;

        public Vector3[] linePoints;
        public int numPoints;

        private void OnEnable()
        {
            if (_handRay == null)
            {
                _handRay = FindObjectOfType<WristShoulderHandRay>();
                if (_handRay == null)
                {
                    Debug.LogWarning("HandRayParabolicLineRenderer needs a HandRay");
                    return;
                }
            }
            _handRay.OnHandRayFrame += UpdateRayInteractor;
        }

        private void OnDisable()
        {
            if (_handRay == null)
            {
                return;
            }

            _handRay.OnHandRayFrame -= UpdateRayInteractor;
        }

        private void UpdateRayInteractor(HandRayDirection handRayDirection)
        {
            if (_rayFrozen)
            {
                return;
            }

            UpdateRayInteractorLogic(handRayDirection, out RaycastHit[] result);
            OnRaycastUpdate?.Invoke(result);
        }

        protected abstract int UpdateRayInteractorLogic(HandRayDirection handRayDirection, out RaycastHit[] results);

        public void FreezeRay()
        {
            _rayFrozen = true;
        }

        public void UnfreezeRay()
        {
            _rayFrozen = false;
        }
    }
}