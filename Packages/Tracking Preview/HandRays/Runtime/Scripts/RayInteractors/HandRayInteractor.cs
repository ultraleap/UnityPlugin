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
        public Action<RaycastHit[], RaycastHit> OnRaycastUpdate;

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
            UpdateRayInteractorLogic(handRayDirection, out RaycastHit[] allHits, out RaycastHit primaryHit);
            OnRaycastUpdate?.Invoke(allHits, primaryHit);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handRayDirection"></param>
        /// <param name="allHits"></param>
        /// <param name="primaryHit"></param>
        /// <returns></returns>
        protected abstract int UpdateRayInteractorLogic(HandRayDirection handRayDirection, out RaycastHit[] allHits, out RaycastHit primaryHit);
    }
}