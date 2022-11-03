using System;
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

        [Header("Events")]
        public Action<RaycastHit[]> OnRaycastUpdate;

        [HideInInspector] public Vector3[] linePoints;
        [HideInInspector] public int numPoints;

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
            UpdateRayInteractorLogic(handRayDirection, out RaycastHit[] result);
            OnRaycastUpdate?.Invoke(result);
        }

        protected abstract int UpdateRayInteractorLogic(HandRayDirection handRayDirection, out RaycastHit[] results);
    }
}